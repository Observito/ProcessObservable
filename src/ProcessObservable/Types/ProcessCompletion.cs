using System;
using System.Linq;
using System.Text;

namespace Observito.Diagnostics.Types
{
    /// <summary>
    /// Process completion data.
    /// </summary>
    public class ProcessCompletion
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ProcessCompletion(int? processId, int? exitCode, bool isDisposed, Exception error, DataLine[] data)
        {
            ProcessId = processId;
            ExitCode = exitCode;
            IsDisposed = isDisposed;
            Error = error;
            _data = data;
        }

        private readonly DataLine[] _data;

        /// <summary>
        /// Optional process id.
        /// </summary>
        public int? ProcessId { get; }

        /// <summary>
        /// Optional exit code.
        /// </summary>
        public int? ExitCode { get; }

        /// <summary>
        /// Was the process disposed?
        /// </summary>
        public bool IsDisposed { get; }

        /// <summary>
        /// Was an error thrown?
        /// </summary>
        public Exception Error { get; }

        /// <summary>
        /// Output data.
        /// </summary>
        public DataLine[] Data => _data.ToArray();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"ProcessCompletion(PID={ProcessId}; ExitCode={ExitCode}; IsDisposed={IsDisposed})");
            if (_data?.Any() == true)
            {
                sb.AppendLine("{");
                foreach (var line in _data)
                    sb.AppendLine($" {line}");
                sb.AppendLine("}");
            }
            if (Error != null)
            {
                sb.AppendLine("====================");
                sb.AppendLine("Error:");
                sb.AppendLine("====================");
                sb.AppendLine(Error.ToString());
                sb.AppendLine("====================");
            }
            return sb.ToString();
        }
    }
}
