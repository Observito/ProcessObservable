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
    // TODO ProcessObservableBuilder

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
        /// <exception cref="ArgumentOutOfRangeException">If the file type is not supported or the file type cannot be determined</exception>
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static IObservable<ProcessSignal> TryCreateFromFile(string fileName, string arguments = null, Action<Process> customizer = null, bool failfast = false)
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
        /// <param name="arguments">Optional arguments to executable; arguments must be escaped as per normal cmd rules</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <param name="failfast">If true exceptions at subscription time are rethrown, 
        /// otherwise subscription exceptions are materialized as an OnError call.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the fileName is invalid</exception>
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static IObservable<ProcessSignal> CreateFromScriptFile(string fileName, string arguments = null, Action<Process> customizer = null, bool failfast = false)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));
            var argStr = "";
            var argStrBuilder = new StringBuilder();
            if (string.IsNullOrWhiteSpace(arguments))
                argStrBuilder.Append($"/C \"\"{fileName}\"\"");
            else
                argStrBuilder.Append($"/C \"\"{fileName} {arguments}\"\"");
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
            }, failfast: failfast);
        }

        private static IObservable<ProcessSignal> Create(Func<Process> factory, bool failfast)
        {
            return Observable.Create<ProcessSignal>(observer =>
            {
                // TODO is the process hanging?

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
                            observer.OnNext(ProcessSignal.FromDisposed(procId.Value));
                            subscription.Dispose();
                        }
                    );

                // Subscribe to the exited event
                var exitedDubscription =
                    process.ExitedObservable().Subscribe(
                        onNext: _ =>
                        {
                            observer.OnNext(ProcessSignal.FromExited(procId.Value, process.ExitCode));
                            subscription.Dispose();
                        }
                    );

                // Subscribe to the output data received event
                var outputDataReceivedSubscription =
                    process.OutputDataReceivedObservable().Where(ev => ev.EventArgs.Data != null).Subscribe(
                        onNext: ev =>
                        {
                            observer.OnNext(ProcessSignal.FromOutput(procId.Value, ev.EventArgs.Data));
                        }
                    );

                // Subscribe to the error data received event
                var errorDataReceivedSubscription =
                    process.ErrorDataReceivedObservable().Where(ev => ev.EventArgs.Data != null).Subscribe(
                        onNext: ev =>
                        {
                            // In the case where a subscription error happened, the process id does not exist
                            // - that scenario is modelled as an OnError signal.
                            observer.OnNext(ProcessSignal.FromError(procId, ev.EventArgs.Data));
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
                    if (failfast)
                        throw ctx;
                    observer.OnError(ctx);
                }

                // Start capturing output and error streams
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // The result is a disposable object representing the subscription
                return subscription;
            });
        }

        private static string EscapeArgument(string argument)
        {
            /*
             *   If /C or /K is specified, then the remainder of the command line after
             *   the switch is processed as a command line, where the following logic is
             *   used to process quote (") characters:
             *
             *      1.  If all of the following conditions are met, then quote characters
             *          on the command line are preserved:
             *
             *          - no /S switch
             *          - exactly two quote characters
             *          - no special characters between the two quote characters,
             *            where special is one of: &<>()@^|
             *          - there are one or more whitespace characters between the
             *            two quote characters
             *          - the string between the two quote characters is the name
             *            of an executable file.
             *
             *      2.  Otherwise, old behavior is to see if the first character is
             *          a quote character and if so, strip the leading character and
             *          remove the last quote character on the command line, preserving
             *          any text after the last quote character.
            */
            //const string SpecialCharacters = "&<> ()@^|";
            throw new NotImplementedException();
        }
    }
}
