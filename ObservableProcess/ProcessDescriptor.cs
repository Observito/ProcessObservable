using System;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace ObservableProcess
{
    public static class ProcessDescriptor
    {
        /// <summary>
        /// Create a new non-running process from the given start info.
        /// </summary>
        /// <param name="info">The process start info</param>
        /// <returns>The new process</returns>
        public static Process ToProcess(this ProcessStartInfo info, Action<Process> customizer = null)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var process = new Process() { StartInfo = info };
            customizer?.Invoke(process);
            return process;
        }

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
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                ErrorDialog = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
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
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                ErrorDialog = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
        }
    }
}
