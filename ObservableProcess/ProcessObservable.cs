using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
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
        /// Creates a process that can be observed for its side-effects of an executable or script.
        /// </summary>
        /// <param name="fileName">File to run</param>
        /// <param name="arguments">Optional arguments to executable or script; arguments must be escaped as per normal cmd rules</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <exception cref="ArgumentOutOfRangeException">If the file type is not supported or the file type cannot be determined</exception>
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static Process TryCreateProcessFromFile(string fileName, string arguments = null)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (ext == ".exe")
                return CreateProcessFromExecutableFile(fileName, arguments);
            else
                return CreateProcessFromScriptFile(fileName, arguments);
        }

        /// <summary>
        /// Creates a process that can be observed for its side-effects of a script.
        /// </summary>
        /// <param name="fileName">Script file to run</param>
        /// <param name="arguments">Optional arguments to executable; arguments must be escaped as per normal cmd rules</param>
        /// <exception cref="ArgumentOutOfRangeException">If the fileName is invalid</exception>
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static Process CreateProcessFromScriptFile(string fileName, string arguments = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            // Convert arguments to cmd.exe arguments
            var argStr = "";
            var argStrBuilder = new StringBuilder();
            if (string.IsNullOrWhiteSpace(arguments))
                argStrBuilder.Append($"/C \"\"{fileName}\"\"");
            else
                argStrBuilder.Append($"/C \"\"{fileName} {arguments}\"\"");
            argStr = argStrBuilder.ToString();

            // Create the process
            return new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = argStr,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                },
            };
        }

        /// <summary>
        /// Creates a process that can be observed for its side-effects.
        /// </summary>
        /// <param name="fileName">Executable file to run</param>
        /// <param name="arguments">Optional arguments to script; arguments must be escaped as per normal cmd rules</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <exception cref="ArgumentOutOfRangeException">If the fileName is invalid</exception>
        /// <returns>A process that can be observed for its side-effects</returns> 
        public static Process CreateProcessFromExecutableFile(string fileName, string arguments = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            // Create the process
            return new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo()
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                },
            };
        }

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
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static Task<ProcessCompletion> StartTaskFromFile(string fileName, string arguments = null, Action<Process> customizer = null, bool failfast = false, CancellationToken? token = null, IProgress<ProcessSignal> progress = null)
        {
            var io = TryCreateFromFile(fileName, arguments, customizer, failfast);
            return io.StartTask(token, progress);
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
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static IObservable<ProcessSignal> TryCreateFromFile(string fileName, string arguments = null, Action<Process> customizer = null, bool failfast = false)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            return CreateFromProcessFactory(() =>
            {
                var proc = TryCreateProcessFromFile(fileName, arguments);
                customizer?.Invoke(proc);
                return proc;
            }, failfast: failfast);
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
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static IObservable<ProcessSignal> CreateFromScriptFile(string fileName, string arguments = null, Action<Process> customizer = null, bool failfast = false)
        {
            return CreateFromProcessFactory(() =>
            {
                var proc = CreateProcessFromScriptFile(fileName, arguments);
                customizer?.Invoke(proc);
                return proc;
            }, failfast: failfast);
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
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static IObservable<ProcessSignal> CreateFromExecutableFile(string fileName, string arguments = null, Action<Process> customizer = null, bool failfast = false)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            return CreateFromProcessFactory(() =>
            {
                var proc = CreateProcessFromExecutableFile(fileName, arguments);
                customizer?.Invoke(proc);
                return proc;
            }, failfast: failfast);
        }

        /// <summary>
        /// Creates a new process observable from a process factory. Note that this method does not try to auto-correct
        /// its input. The process factory must enable 
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="failfast"></param>
        /// <returns></returns>
        public static IObservable<ProcessSignal> CreateFromProcessFactory(Func<Process> factory, bool failfast)
        {
            return Observable.Create<ProcessSignal>(observer =>
            {
                // Create the new process
                Process process = factory();

                // Setup a subscription result that captures inner subscriptions to events so they can all
                // be disposed together by the subcriber
                var subscriptions = new CompositeDisposable();

                // Need to capture the process id, so it can be returned in an event as metadata even when 
                // the subscription is disposed 
                int? procId = null;

                //Test pre-emptive disposal:
                //Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t => { proc.Dispose(); });

                // Subscribe to the Disposed event
                subscriptions.Add(
                    process.DisposedObservable().Subscribe(
                        onNext: _ =>
                        {
                            observer.OnNext(ProcessSignal.FromDisposed(procId.Value));
                            subscriptions.Dispose();
                        }
                    )
                );

                // Subscribe to the Exited event
                subscriptions.Add(
                    process.ExitedObservable().Subscribe(
                        onNext: _ =>
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
                        process.OutputDataReceivedObservable().Where(ev => ev.EventArgs.Data != null)
                        .Subscribe(
                            onNext: ev =>
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
                        process.ErrorDataReceivedObservable().Where(ev => ev.EventArgs.Data != null).Subscribe(
                            onNext: ev =>
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

                // Start the process
                try
                {
                    process.Start();

                    // Capture the process id -- cannot get when disposed
                    procId = process.Id;
                }
                catch (Exception ex)
                {
                    var ctx = new Exception("Error subscribing to ProcessObservable", ex);
                    ctx.Data.Add("ProcessId", procId);
                    if (failfast)
                        throw ctx;
                    // Exception => IObservable.OnError
                    observer.OnError(ctx);
                    subscriptions.Dispose();

                    subscriptions.Clear();

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
            });
        }
    }
}
