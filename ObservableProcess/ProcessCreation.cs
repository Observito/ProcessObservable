using System;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace ObservableProcess
{
    public static class ProcessCreation
    {
        /// <summary>
        /// Creates a process that can be observed for its side-effects of an executable or script.
        /// </summary>
        /// <param name="fileName">File to run</param>
        /// <param name="arguments">Optional arguments to executable or script; arguments must be escaped as per normal cmd rules</param>
        /// <param name="customizer">Optional process customizer</param>
        /// <exception cref="ArgumentOutOfRangeException">If the file type is not supported or the file type cannot be determined</exception>
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static Process TryCreateFromFile(string fileName, string arguments = null)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (ext == ".exe")
                return CreateFromExecutableFile(fileName, arguments);
            else
                return CreateFromScriptFile(fileName, arguments);
        }

        /// <summary>
        /// Creates a process that can be observed for its side-effects of a script.
        /// </summary>
        /// <param name="fileName">Script file to run</param>
        /// <param name="arguments">Optional arguments to executable; arguments must be escaped as per normal cmd rules</param>
        /// <exception cref="ArgumentOutOfRangeException">If the fileName is invalid</exception>
        /// <returns>An observable that can observe the side-effects of a process</returns> 
        public static Process CreateFromScriptFile(string fileName, string arguments = null)
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
        public static Process CreateFromExecutableFile(string fileName, string arguments = null)
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
    }
}
