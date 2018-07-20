using System.Linq;
using System.Text;

namespace ObservableProcess
{
    /// <summary>
    /// Process completion data.
    /// </summary>
    public class ProcessCompletion
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ProcessCompletion(int? processId, int? exitCode, bool isDisposed, DataLine[] data)
        {
            ProcessId = processId;
            ExitCode = exitCode;
            IsDisposed = isDisposed;
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
            return sb.ToString();
        }
    }
}
