using System;
using System.Collections.Generic;
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

                foreach (var signal in observable.ToEnumerable())
                {
                    // Cancel task if needed
                    token?.ThrowIfCancellationRequested();

                    // Collect result info
                    switch (signal.Type)
                    {
                        case ProcessSignalClassifier.Started:
                            processId = processId ?? signal.ProcessId;
                            break;
                        case ProcessSignalClassifier.Exited:
                            processId = processId ?? signal.ProcessId;
                            exitCode = signal.ExitCode;
                            break;
                        case ProcessSignalClassifier.Disposed:
                            processId = processId ?? signal.ProcessId;
                            isDisposed = true;
                            break;
                        case ProcessSignalClassifier.Output:
                            processId = processId ?? signal.ProcessId;
                            data.Add(new DataLine(DataLineType.Output, signal.Data, DateTime.Now, counter));
                            counter++;
                            break;
                        case ProcessSignalClassifier.Error:
                            processId = processId ?? signal.ProcessId;
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

                // Completed, return result
                return new ProcessCompletion(
                    processId: processId, 
                    exitCode: exitCode, 
                    isDisposed: isDisposed, 
                    data: data.ToArray());
            });
        }
    }
}
