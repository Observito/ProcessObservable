using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
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
        /// <param name="arguments">Optional arguments to executable or script</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <exception cref="ArgumentOutOfRangeException">If the file type is not supported or the file type cannot be determined</exception>
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static IObservable<ProcessSignal> TryCreateFromFile(string fileName, string arguments = null, Action<Process> customizer = null)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (ext == ".exe")
                return CreateFromExecutableFile(fileName, arguments, customizer);
            else
                return CreateFromScriptFile(fileName, arguments, customizer);
        }

        /// <summary>
        /// Creates a new observable that can observe Process side-effects of a script.
        /// </summary>
        /// <param name="fileName">Script file to run</param>
        /// <param name="arguments">Optional arguments to executable</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <exception cref="ArgumentOutOfRangeException">If the fileName is invalid</exception>
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static IObservable<ProcessSignal> CreateFromScriptFile(string fileName, string arguments = null, Action<Process> customizer = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));
            var argStr = "";
            var argStrBuilder = new StringBuilder();
            argStrBuilder.Append($"/C {fileName}");
            if (arguments != null)
                argStrBuilder.Append($" {arguments}");
            argStr = argStrBuilder.ToString();

            // Need to wrap the process creation -- otherwise the same process will be setup 
            // once and reused when the next subscription happens
            return Create(() =>
            {
                var process = new Process()
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
                customizer?.Invoke(process);
                return process;
            });
        }

        /// <summary>
        /// Creates a new observable that can observe Process side-effects.
        /// </summary>
        /// <param name="fileName">Executable file to run</param>
        /// <param name="arguments">Optional arguments to script</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <exception cref="ArgumentOutOfRangeException">If the fileName is invalid</exception>
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static IObservable<ProcessSignal> CreateFromExecutableFile(string fileName, string arguments = null, Action<Process> customizer = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            // Need to wrap the process creation -- otherwise the same process will be setup 
            // once and reused when the next subscription happens
            return Create(() =>
            {
                var process = new Process()
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
                customizer?.Invoke(process);
                return process;
            });
        }

        private static IObservable<ProcessSignal> Create(Func<Process> factory)
        {
            return Observable.Create<ProcessSignal>(observer =>
            {
                // Create the new process
                Process process = factory();

                // Setup a subscription result that captures inner subscriptions to events so they can all
                // be disposed together by the subcriber
                var subscription = new CompositeDisposable();

                // Need to capture the process id, so it can be returned in an event as metadata even when 
                // the subscription is disposed 
                int? procId = null;

                //Test pre-emptive disposal:
                //Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t => { proc.Dispose(); });

                // Subscribe to the disposed event
                var disposedSubscription =
                    process.DisposedObservable().Subscribe(
                        onNext: _ =>
                        {
                            observer.OnNext(new ProcessSignal()
                            {
                                ProcessId = procId,
                                Type = ProcessSignalClassifier.Disposed,
                            });
                            subscription.Dispose();
                        }
                    );

                // Subscribe to the exited event
                var exitedDubscription =
                    process.ExitedObservable().Subscribe(
                        onNext: _ =>
                        {
                            observer.OnNext(new ProcessSignal()
                            {
                                ProcessId = procId,
                                Type = ProcessSignalClassifier.Exited,
                                ExitCode = process.ExitCode
                            });
                            subscription.Dispose();
                        }
                    );

                // Subscribe to the output data received event
                var outputDataReceivedSubscription =
                    process.OutputDataReceivedObservable().Where(ev => ev.EventArgs.Data != null).Subscribe(
                        onNext: ev =>
                        {
                            observer.OnNext(new ProcessSignal()
                            {
                                ProcessId = procId,
                                Type = ProcessSignalClassifier.Output,
                                Data = ev.EventArgs.Data
                            });
                        }
                    );

                // Subscribe to the error data received event
                var errorDataReceivedSubscription =
                    process.ErrorDataReceivedObservable().Where(ev => ev.EventArgs.Data != null).Subscribe(
                        onNext: ev =>
                        {
                            observer.OnNext(new ProcessSignal()
                            {
                                ProcessId = procId,
                                Type = ProcessSignalClassifier.Error,
                                Data = ev.EventArgs.Data
                            });
                        }
                    );

                // Add all event subscriptions to the combined subscription
                subscription.Add(disposedSubscription);
                subscription.Add(exitedDubscription);
                subscription.Add(outputDataReceivedSubscription);
                subscription.Add(errorDataReceivedSubscription);

                // Add a notification to the subscriber that the subscription is completed 
                // whenever the subscription is disposed by whatever means
                subscription.Add(Disposable.Create(observer.OnCompleted));

                // Ensures the streams are closed when disposing
                subscription.Add(Disposable.Create(() =>
                {
                    //process.Dispose();
                }));

                // Start the process
                try
                {
                    process.Start();

                    // Capture the process id -- cannot get when disposed
                    procId = process.Id;
                }
                catch (Exception ex)
                {
                    // Exception => IObservable.OnError
                    var ctx = new Exception("Error subscribing to ProcessObservable", ex);
                    ctx.Data.Add("Process", process);
                    observer.OnError(ctx);
                }

                // Start capturing output and error streams
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // The result is a disposable object representing the subscription
                return subscription;
            });
        }
    }
}
