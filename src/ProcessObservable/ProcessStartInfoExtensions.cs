using Observito.Diagnostics.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Observito.Diagnostics
{
    /// <summary>
    /// <see cref="ProcessStartInfo"/> extensions for ProcessObservable.
    /// </summary>
    public static class ProcessStartInfoExtensions
    {
        private static readonly HashSet<string> _commands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cmd", "cmd.exe"
        };
        
        /// <summary>
        /// Checks that the file exists and optionally working directory, if specified.
        /// </summary>
        /// <param name="info">The <see cref="ProcessStartInfo"/></param>
        /// <param name="checkWorkingDirectory">Check working directory exists if not-null</param>
        /// <returns><see cref="ProcessStartInfo"/></returns>
        public static ProcessStartInfo EnsureFileExists(this ProcessStartInfo info, bool checkWorkingDirectory = true)
        {
            if (checkWorkingDirectory && !string.IsNullOrEmpty(info.WorkingDirectory) && !Directory.Exists(info.WorkingDirectory))
                throw new DirectoryNotFoundException(info.WorkingDirectory);

            if (!File.Exists(info.FileName) && !_commands.Contains(info.FileName))
                throw new FileNotFoundException(info.FileName);

            return info;
        }

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

        /// <summary>
        /// Creates a new cloned <see cref="ProcessStartInfo"/> with arguments added.
        /// </summary>
        /// <param name="source">The <see cref="ProcessStartInfo"/></param>
        /// <param name="arguments">Argument list</param>
        /// <returns>The updated <see cref="ProcessStartInfo"/></returns>
        public static ProcessStartInfo WithArguments(this ProcessStartInfo source, params string[] arguments) =>
            source.With(__ => {
                foreach (var arg in arguments)
                    __.ArgumentList.Add(arg);
            });

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
                foreach (string arg in source.ArgumentList)
                    cloned.ArgumentList.Add(arg);
                foreach (string key in source.Environment.Keys)
                    cloned.Environment[key] = source.Environment[key];
                foreach (string key in source.EnvironmentVariables.Keys)
                    cloned.EnvironmentVariables[key] = source.EnvironmentVariables[key];
            });

        /// <summary>
        /// Creates a new process observable from a process factory. Note that this method does not try to auto-correct
        /// its input.
        /// </summary>
        /// <param name="factory">The process factory</param>
        /// <exception cref="ArgumentNullException">If the start info is null</exception>
        /// <returns>The new observable</returns>
        public static IObservable<ProcessSignal> ToObservable(this ProcessStartInfo info, Action<Process> customizer = null)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            return ProcessObservable.FromProcessSource(() => info.ToProcess(customizer));
        }
    }
}
