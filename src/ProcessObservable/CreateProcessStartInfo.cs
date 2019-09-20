using System;
using System.Diagnostics;
using System.IO;

namespace Observito.Diagnostics
{
    /// <summary>
    /// Factory methods.
    /// </summary>
    public static class CreateProcessStartInfo
    {
        /// <summary>
        /// Creates start info for either an executable or an associated/openable file.
        /// </summary>
        /// <param name="fileName">File to run</param>
        /// <param name="arguments">Optional arguments to executable or script; arguments must be escaped as per normal cmd rules</param>
        /// <exception cref="ArgumentOutOfRangeException">If the file type is not supported or the file type cannot be determined</exception>
        /// <returns>A new <see cref="ProcessStartInfo"/></returns> 
        public static ProcessStartInfo FromFile(string fileName, params string[] arguments)
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
        public static ProcessStartInfo FromAssociatedFile(string fileName, params string[] arguments)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            // Create the process
            return new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                UseShellExecute = false,
            }
            .WithArguments("/C", fileName)
            .WithArguments(arguments)
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
        public static ProcessStartInfo FromExecutableFile(string fileName, params string[] arguments)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentOutOfRangeException(nameof(fileName));

            // Create the process
            return new ProcessStartInfo()
            {
                FileName = fileName,
                UseShellExecute = false,
            }
            .WithArguments(arguments)
            .WithGUI(false)
            .WithRedirectIO(true);
        }
    }
}
