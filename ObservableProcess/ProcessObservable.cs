using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ObservableProcess.ProcessExtensions;

namespace ObservableProcess
{
    /// <summary>
    /// Static methods to create observable processes.
    /// </summary>
    public static class ProcessObservable
    {
        /// <summary>
        /// Creates a new observable that can observe Process side-effects of an executable or script.
        /// </summary>
        /// <param name="fileName">File to run</param>
        /// <param name="arguments">Optional arguments to executable or script; arguments must be escaped as per normal cmd rules</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <param name="failfast">If true exceptions at subscription time are rethrown, 
        /// otherwise subscription exceptions are materialized as an OnError call.</param>
        /// <param name="progress">Optional progress reporting</param>
        /// <param name="token">Optional cancellation token</param>
        /// <exception cref="ArgumentOutOfRangeException">If the file type is not supported or the file type cannot be determined</exception>
        /// <returns>The new running task</returns>
        public static Task<ProcessCompletion> RunAsync(string fileName, string arguments = null, Action<Process> customizer = null, bool failfast = false, CancellationToken? token = null, IProgress<ProcessSignal> progress = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            var io = FromFile(fileName, arguments, customizer, failfast);
            return io.RunAsync(token, progress);
        }

        /// <summary>
        /// Creates a new observable that can observe Process side-effects of an executable or script.
        /// </summary>
        /// <param name="fileName">File to run</param>
        /// <param name="arguments">Optional arguments to executable or script; arguments must be escaped as per normal cmd rules</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <param name="failfast">If true exceptions at subscription time are rethrown, 
        /// otherwise subscription exceptions are materialized as an OnError call.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the file type is not supported or the file type cannot be determined</exception>
        /// <returns>The new observable</returns>
        public static IObservable<ProcessSignal> FromFile(string fileName, string arguments = null, Action<Process> customizer = null, bool failfast = false)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            return ProcessDescriptor.FromFile(fileName, arguments).ToObservable(customizer, failfast);
        }

        /// <summary>
        /// Creates a new observable that can observe Process side-effects of a script.
        /// </summary>
        /// <param name="fileName">Script file to run</param>
        /// <param name="arguments">Optional arguments to executable; arguments must be escaped as per normal cmd rules</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <param name="failfast">If true exceptions at subscription time are rethrown, 
        /// otherwise subscription exceptions are materialized as an OnError call.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the fileName is invalid</exception>
        /// <returns>The new observable</returns>
        public static IObservable<ProcessSignal> FromAssociatedFile(string fileName, string arguments = null, Action<Process> customizer = null, bool failfast = false)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            return ProcessDescriptor.FromAssociatedFile(fileName, arguments).ToObservable(customizer, failfast);
        }

        /// <summary>
        /// Creates a new observable that can observe Process side-effects.
        /// </summary>
        /// <param name="fileName">Executable file to run</param>
        /// <param name="arguments">Optional arguments to script; arguments must be escaped as per normal cmd rules</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <param name="failfast">If true exceptions at subscription time are rethrown, 
        /// otherwise subscription exceptions are materialized as an OnError call.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the fileName is invalid</exception>
        /// <returns>The new observable</returns>
        public static IObservable<ProcessSignal> FromExecutableFile(string fileName, string arguments = null, Action<Process> customizer = null, bool failfast = false)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            return FromProcessSource(() =>
            {
                var proc = ProcessDescriptor.FromExecutableFile(fileName, arguments).ToProcess();
                customizer?.Invoke(proc);
                return proc;
            }, failfast: failfast);
        }



        /// <summary>
        /// Creates a new process observable from a process factory. Note that this method does not try to auto-correct
        /// its input. The process factory must enable 
        /// </summary>
        /// <param name="factory">The process factory</param>
        /// <param name="failfast">If true then exceptions at process start time will be propagated; if false they will be materialized as an OnError case</param>
        /// <exception cref="ArgumentNullException">If the start info is null</exception>
        /// <returns>The new observable</returns>
        public static IObservable<ProcessSignal> ToObservable(this ProcessStartInfo info, Action<Process> customizer = null, bool failfast = false)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            return FromProcessSource(() => info.ToProcess(customizer), failfast);
        }

        /// <summary>
        /// Creates a new process observable from a process factory. Note that this method does not try to auto-correct
        /// its input. The process factory must enable 
        /// </summary>
        /// <param name="source">The process factory</param>
        /// <param name="failfast">If true then exceptions at process start time will be propagated; if false they will be materialized as an OnError case</param>
        /// <exception cref="ArgumentNullException">If the process factory is null</exception>
        /// <exception cref="Exception">If failfast and an eror occurs during process start (during subscription)</exception>
        /// <returns>The created process observable</returns>
        public static IObservable<ProcessSignal> FromProcessSource(Func<Process> source, bool failfast = false)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return Observable.Create<ProcessSignal>(observer =>
            {
                // Create the new process
                var process = source();

                // Ensure we can subscribe to process events
                process.EnableRaisingEvents = true;

                // Setup a subscription result that captures inner subscriptions to events so they can all
                // be disposed together by the subcriber
                var subscriptions = new CompositeDisposable();

                try
                {
                    // Need to capture the process id, so it can be returned in an event as metadata even when 
                    // the subscription is disposed 
                    int? procId = null;

                    //Test pre-emptive disposal:
                    //Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t => { proc.Dispose(); });

                    // Coordinate completion signals
                    IObservable<ProcessCoordinationSignalType> completionObservable = null;
                    {
                        var coordinated = new List<IObservable<ProcessSignalType>>();
                        if (process.StartInfo.RedirectStandardOutput)
                            coordinated.Add(process.OutputDataReceivedObservable().Where(x => x.EventArgs.Data == null).Select(_ => ProcessSignalType.OutputDataDone));
                        if (process.StartInfo.RedirectStandardError)
                            coordinated.Add(process.ErrorDataReceivedObservable().Where(x => x.EventArgs.Data == null).Select(_ => ProcessSignalType.ErrorDataDone));
                        coordinated.Add(process.ExitedObservable().Select(_ => ProcessSignalType.Exited));
                        coordinated.Add(process.DisposedObservable().Select(_ => ProcessSignalType.Disposed));
                        completionObservable =
                            Observable.Merge(coordinated)
                            .Select(signalType => ProcessCoordinationSignalType.Of(signalType))
                            .Scan((a, b) => a + b)
                            .Where(x => x.Completed);
                    }

                    // Dispose subscriptions 
                    subscriptions.Add(
                        completionObservable
                        .Subscribe(
                            onNext: ev =>
                            {
                                subscriptions.Dispose();
                                if (ev.Exited)
                                {
                                    observer.OnNext(ProcessSignal.FromExited(procId.Value, process.ExitCode));
                                }
                                else if (ev.Disposed)
                                {
                                    observer.OnNext(ProcessSignal.FromDisposed(procId.Value));
                                }
                                else
                                {
                                    Debug.WriteLine("Implementation error: expected exited or dispose - found neither");
                                }
                                observer.OnCompleted();
                            }
                        )
                    );

                    // Subscribe to the OutputDataReceived event
                    if (process.StartInfo.RedirectStandardOutput)
                    {
                        subscriptions.Add(
                            process.OutputDataReceivedObservable()
                            .Subscribe(
                                onNext: ev =>
                                {
                                    if (ev.EventArgs.Data == null)
                                        observer.OnNext(ProcessSignal.FromOutputDataDone(procId.Value));
                                    else
                                        observer.OnNext(ProcessSignal.FromOutputData(procId.Value, ev.EventArgs.Data));
                                }
                            )
                        );
                    }

                    // Subscribe to the ErrorDataReceived event
                    if (process.StartInfo.RedirectStandardError)
                    {
                        subscriptions.Add(
                            process.ErrorDataReceivedObservable()
                            .Subscribe(
                                onNext: ev =>
                                {
                                    // In the case where a subscription error happened, the process id does not exist
                                    // - that scenario is modelled as an OnError signal.
                                    if (ev.EventArgs.Data == null)
                                        observer.OnNext(ProcessSignal.FromErrorDataDone(procId.Value));
                                    else
                                        observer.OnNext(ProcessSignal.FromErrorData(procId, ev.EventArgs.Data));
                                }
                            )
                        );
                    }

                    // Attempt to start the process
                    try
                    {
                        process.Start();

                        // Capture the process id -- cannot get when disposed
                        procId = process.Id;

                        // Inform observer
                        observer.OnNext(ProcessSignal.FromStarted(procId.Value));
                    }
                    catch (Exception ex)
                    {
                        var ctx = new Exception("Error subscribing to ProcessObservable", ex);
                        ctx.Data.Add("ProcessId", procId);
                        if (failfast)
                        {
                            // Propagate error
                            throw ctx;
                        }

                        // Exception => IObservable.OnError
                        observer.OnError(ctx);

                        // Dispose all subscriptions
                        subscriptions.Dispose();

                        return subscriptions;
                    }

                    // Start capturing standard output asynchronously
                    if (process.StartInfo.RedirectStandardOutput)
                        process.BeginOutputReadLine();

                    // Start capturing the standard error asynchronously
                    if (process.StartInfo.RedirectStandardError)
                        process.BeginErrorReadLine();

                    // The result is a disposable object representing the subscription
                    return subscriptions;
                }
                catch
                {
                    // Dispose subscriptions
                    subscriptions.Dispose();

                    // Propagate error
                    throw;
                }
            });
        }
    }
}
