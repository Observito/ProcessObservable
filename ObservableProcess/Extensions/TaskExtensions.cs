using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ObservableProcess
{
    /// <summary>
    /// Process observable extensions for tasks.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Convert the process observable to a process completion task.
        /// The task can optionally report progress and will upon completion return collected completion data.
        /// </summary>
        /// <param name="observable">The process observable</param>
        /// <param name="token">Optional cancellation token</param>
        /// <param name="progress">Optional progress to report to</param>
        /// <returns>Process completion data</returns>
        public static Task<ProcessCompletion> RunAsync(this IObservable<ProcessSignal> observable, CancellationToken? token = null, IProgress<ProcessSignal> progress = null)
        {
            var task = observable.AsTask();
            task.Start();
            return task;
        }

        /// <summary>
        /// Convert the process observable to a process completion task.
        /// The task can optionally report progress and will upon completion return collected completion data.
        /// </summary>
        /// <param name="observable">The process observable</param>
        /// <param name="token">Optional cancellation token</param>
        /// <param name="progress">Optional progress to report to</param>
        /// <returns>Process completion data</returns>
        public static Task<ProcessCompletion> AsTask(this IObservable<ProcessSignal> observable, CancellationToken? token = null, IProgress<ProcessSignal> progress = null)
        {
            return new Task<ProcessCompletion>(() =>
            {
                // Cancel task if needed
                token?.ThrowIfCancellationRequested();

                // Temporaries to collect result data
                var processId = default(int?);
                var exitCode = default(int?);
                var isDisposed = false;
                var data = new List<DataLine>();
                var counter = 0;

                Exception exc = null;
                try
                {
                    foreach (var signal in observable.ToEnumerable())
                    {
                        // Cancel task if needed
                        token?.ThrowIfCancellationRequested();

                        // Collect process id
                        processId = processId ?? signal.ProcessId;

                        // Collect result info
                        switch (signal.Type)
                        {
                            case ProcessSignalType.Started:
                                break;
                            case ProcessSignalType.Exited:
                                exitCode = signal.ExitCode;
                                break;
                            case ProcessSignalType.Disposed:
                                isDisposed = true;
                                break;
                            case ProcessSignalType.OutputData:
                                data.Add(new DataLine(DataLineType.Output, signal.Data, DateTime.Now, counter));
                                counter++;
                                break;
                            case ProcessSignalType.ErrorData:
                                data.Add(new DataLine(DataLineType.Error, signal.Data, DateTime.Now, counter));
                                counter++;
                                break;
                        }

                        // Report task progress if requested
                        if (progress != null)
                        {
                            Task.Run(() => progress.Report(signal));
                        }
                    }
                }
                catch (Exception ex) // OnError case
                {
                    exc = ex;
                }

                // Completed, return result
                return new ProcessCompletion(
                    processId: processId, 
                    exitCode: exitCode, 
                    isDisposed: isDisposed, 
                    error: exc,
                    data: data.ToArray());
            });
        }
    }
}
