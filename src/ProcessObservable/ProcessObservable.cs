using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Observito.Diagnostics.Types;

namespace Observito.Diagnostics
{
    /// <summary>
    /// Static methods to create observable processes.
    /// </summary>
    public static class ProcessObservable
    {
        /// <summary>
        /// Creates a new process observable from a process factory. Note that this method does not try to auto-correct
        /// its input.
        /// </summary>
        /// <param name="source">The process factory</param>
        /// <exception cref="ArgumentNullException">If the process factory is null</exception>
        /// <returns>The created process observable</returns>
        public static IObservable<ProcessSignal> FromProcessSource(Func<Process> source)
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

                    // Need to capture the exit code, so it can be returned as metadata
                    int? exitCode = null;

                    subscriptions.Add(process.ExitedObservable().Subscribe(x => exitCode = process.ExitCode));

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
                        var initial = new ProcessCoordinationSignalType()
                        {
                            OutputDataDone = !process.StartInfo.RedirectStandardOutput,
                            ErrorDataDone = !process.StartInfo.RedirectStandardError,
                        };
                        completionObservable =
                            Observable.Merge(coordinated)
                            .Select(signalType => ProcessCoordinationSignalType.Of(signalType))
                            .Scan(initial, (a, b) => a + b)
                            .Where(x => x.Completed);
                    }

                    // Dispose subscriptions 
                    subscriptions.Add(
                        completionObservable
                        .Subscribe(
                            onNext: ev =>
                            {
                                if (ev.Exited)
                                {
                                    observer.OnNext(ProcessSignal.FromExited(procId.Value, exitCode ?? 0));
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
                                subscriptions.Dispose();
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

                        // Propagate error
                        throw ctx;
                    }

                    // Start capturing standard output asynchronously
                    if (process.StartInfo.RedirectStandardOutput)
                        process.BeginOutputReadLine();

                    // Start capturing the standard error asynchronously
                    if (process.StartInfo.RedirectStandardError)
                        process.BeginErrorReadLine();

                    // The result is a disposable that will dispose the process
                    return Disposable.Create(() =>
                    {
                        process.Dispose();
                    });
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
