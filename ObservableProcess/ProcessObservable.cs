using System;
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

            return System.Reactive.Linq.Observable.Create<ProcessSignal>((IObserver<ProcessSignal> observer) =>
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

                    // Subscribe to the Disposed event
                    subscriptions.Add(
                        process.DisposedObservable().Subscribe(
                            onNext: (System.Reactive.EventPattern<object> _) =>
                            {
                                observer.OnNext(ProcessSignal.FromDisposed(procId.Value));
                                subscriptions.Dispose();
                            }
                        )
                    );

                    // Subscribe to the Exited event
                    subscriptions.Add(
                        process.ExitedObservable().Subscribe(
                            onNext: (System.Reactive.EventPattern<object> _) =>
                            {
                                observer.OnNext(ProcessSignal.FromExited(procId.Value, process.ExitCode));
                                subscriptions.Dispose();
                            }
                        )
                    );

                    // Subscribe to the OutputDataReceived event
                    if (process.StartInfo.RedirectStandardOutput)
                    {
                        subscriptions.Add(
                            process.OutputDataReceivedObservable().Where((System.Reactive.EventPattern<DataReceivedEventArgs> ev) => ev.EventArgs.Data != null)
                            .Subscribe(
                                onNext: (System.Reactive.EventPattern<DataReceivedEventArgs> ev) =>
                                {
                                    observer.OnNext(ProcessSignal.FromOutput(procId.Value, ev.EventArgs.Data));
                                }
                            )
                        );
                    }

                    // Subscribe to the ErrorDataReceived event
                    if (process.StartInfo.RedirectStandardError)
                    {
                        subscriptions.Add(
                            process.ErrorDataReceivedObservable().Where((System.Reactive.EventPattern<DataReceivedEventArgs> ev) => ev.EventArgs.Data != null).Subscribe(
                                onNext: (System.Reactive.EventPattern<DataReceivedEventArgs> ev) =>
                                {
                                    // In the case where a subscription error happened, the process id does not exist
                                    // - that scenario is modelled as an OnError signal.
                                    observer.OnNext(ProcessSignal.FromError(procId, ev.EventArgs.Data));
                                }
                            )
                        );
                    }

                    // Add a notification to the subscriber that the subscription is completed 
                    // whenever the subscription is disposed by whatever means
                    subscriptions.Add(Disposable.Create(observer.OnCompleted));

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
