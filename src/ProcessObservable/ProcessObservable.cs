using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Observito.Diagnostics.Types;

namespace Observito.Diagnostics
{
    /*
    public static class ProcessDescriptor
    {
        #region Clone/Update/With
        /// <summary>
        /// Update source with the given customizations.
        /// </summary>
        /// <param name="source">The source <see cref="ProcessStartInfo"/></param>
        /// <param name="customizer">The customizations to apply</param>
        /// <returns>The updated <see cref="ProcessStartInfo"/></returns>
        public static ProcessStartInfo Update(this ProcessStartInfo source, Action<ProcessStartInfo> customizer)
        {
            customizer?.Invoke(source);
            return source;
        }

        /// <summary>
        /// Create a clone of <param name="source"/> with the given customizations.
        /// </summary>
        /// <param name="source">The source <see cref="ProcessStartInfo"/></param>
        /// <param name="customizer">The customizations to apply</param>
        /// <returns>The cloned and customized version of <paramref name="source"/></returns>
        public static ProcessStartInfo With(this ProcessStartInfo source, Action<ProcessStartInfo> customizer) =>
            source.Clone().Update(customizer);

        /// <summary>
        /// Clones the source <see cref="ProcessStartInfo"/>.
        /// </summary>
        /// <param name="source">The <see cref="ProcessStartInfo"/> to clone.</param>
        /// <returns>The cloned <see cref="ProcessStartInfo"/>.</returns>
        public static ProcessStartInfo Clone(this ProcessStartInfo source) =>
            new ProcessStartInfo()
            {
                FileName = source.FileName,
                Arguments = source.Arguments,
                WorkingDirectory = source.WorkingDirectory,
                Domain = source.Domain,
                UserName = source.UserName,
                Password = source.Password,
                LoadUserProfile = source.LoadUserProfile,
                UseShellExecute = source.UseShellExecute,
                ErrorDialog = source.ErrorDialog,
                ErrorDialogParentHandle = source.ErrorDialogParentHandle,
                CreateNoWindow = source.CreateNoWindow,
                WindowStyle = source.WindowStyle,
                RedirectStandardError = source.RedirectStandardError,
                RedirectStandardInput = source.RedirectStandardInput,
                RedirectStandardOutput = source.RedirectStandardOutput,
                StandardErrorEncoding = source.StandardErrorEncoding,
                StandardOutputEncoding = source.StandardOutputEncoding,
                Verb = source.Verb,
            }
            .Update(cloned =>
            {
                foreach (string key in source.Environment.Keys)
                    cloned.Environment[key] = source.Environment[key];
                foreach (string key in source.EnvironmentVariables.Keys)
                    cloned.EnvironmentVariables[key] = source.EnvironmentVariables[key];
            });
        #endregion

        #region Common customizations
        /// <summary>
        /// Creates a new cloned <see cref="ProcessStartInfo"/> with either redirect all IO or not.
        /// </summary>
        /// <param name="source">The <see cref="ProcessStartInfo"/></param>
        /// <param name="redirect">Should all IO be redirected? (StandardInput, StandardOutput, StandardError)</param>
        /// <returns>The updated <see cref="ProcessStartInfo"/></returns>
        public static ProcessStartInfo WithRedirectIO(this ProcessStartInfo source, bool redirect) =>
            source.With(__ => {
                __.RedirectStandardInput = redirect;
                __.RedirectStandardOutput = redirect;
                __.RedirectStandardError = redirect;
            });

        /// <summary>
        /// Creates a new cloned <see cref="ProcessStartInfo"/> with either UI or not.
        /// </summary>
        /// <param name="source">The <see cref="ProcessStartInfo"/></param>
        /// <param name="gui">Should all IO be redirected? (StandardInput, StandardOutput, StandardError)</param>
        /// <returns>The updated <see cref="ProcessStartInfo"/></returns>
        public static ProcessStartInfo WithGUI(this ProcessStartInfo source, bool gui) =>
            source.With(__ => {
                __.ErrorDialog = gui;
                __.WindowStyle = gui ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
                __.CreateNoWindow = !gui;
            });
        #endregion

        #region Creators
        /// <summary>
        /// Creates start info for either an executable or an associated/openable file.
        /// </summary>
        /// <param name="fileName">File to run</param>
        /// <param name="arguments">Optional arguments to executable or script; arguments must be escaped as per normal cmd rules</param>
        /// <exception cref="ArgumentOutOfRangeException">If the file type is not supported or the file type cannot be determined</exception>
        /// <returns>A new <see cref="ProcessStartInfo"/></returns> 
        public static ProcessStartInfo FromFile(string fileName, string arguments = null)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (ext == ".exe")
                return FromExecutableFile(fileName, arguments);
            else
                return FromAssociatedFile(fileName, arguments);
        }

        /// <summary>
        /// Creates <see cref="ProcessStartInfo"/> for an associated/openable file.
        /// </summary>
        /// <param name="fileName">Script file to run</param>
        /// <param name="arguments">Optional arguments to executable; arguments must be escaped as per normal cmd rules</param>
        /// <exception cref="ArgumentOutOfRangeException">If the fileName is invalid</exception>
        /// <returns>A new <see cref="ProcessStartInfo"/></returns> 
        public static ProcessStartInfo FromAssociatedFile(string fileName, string arguments = null)
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
            return new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = argStr,
                UseShellExecute = false,
            }
            .WithGUI(false)
            .WithRedirectIO(true);
        }

        /// <summary>
        /// Creates <see cref="ProcessStartInfo"/> from an executable file.
        /// </summary>
        /// <param name="fileName">Executable file to run</param>
        /// <param name="arguments">Optional arguments to script; arguments must be escaped as per normal cmd rules</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <exception cref="ArgumentOutOfRangeException">If the fileName is invalid</exception>
        /// <returns>A new <see cref="ProcessStartInfo"/></returns> 
        public static ProcessStartInfo FromExecutableFile(string fileName, string arguments = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            // Create the process
            return new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
            }
            .WithGUI(false)
            .WithRedirectIO(true);
        }
        #endregion

        #region Conversions
        /// <summary>
        /// Create a new non-running process from the given start info.
        /// </summary>
        /// <param name="info">The <see cref="ProcessStartInfo"/></param>
        /// <returns>The new process</returns>
        public static Process ToProcess(this ProcessStartInfo info, Action<Process> customizer = null)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var process = new Process() { StartInfo = info.Clone() };
            customizer?.Invoke(process);
            return process;
        }
        #endregion
    }
    */













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
