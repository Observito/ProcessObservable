using System;

namespace ObservableProcess
{
    /// <summary>
    /// A signal from a process observable. 
    /// Signals carry all captured observable side-effects from a process.
    /// </summary>
    public class ProcessSignal
    {
        /// <summary>
        /// Classification of the signal type.
        /// </summary>
        public ProcessSignalClassifier Type { get; set; }

        /// <summary>
        /// The id of the specific process that this signal is observed from.
        /// </summary>
        public int? ProcessId { get; set; }

        /// <summary>
        /// Optional data from the process. 
        /// Relevant for <see cref="ProcessSignalClassifier.Output"/> and <see cref="ProcessSignalClassifier.Error"/>.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// If the process has exited, then this is the process exit code.
        /// Relevant for <see cref="ProcessSignalClassifier.Exited"/>
        /// </summary>
        public int? ExitCode { get; set; }

        /// <summary>
        /// A formatted version of process signal.
        /// </summary>
        /// <returns>The formatted string.</returns>
        public override string ToString()
        {
            switch (Type)
            {
                case ProcessSignalClassifier.Disposed:
                    return $"[PID={ProcessId}]/{Type}";

                case ProcessSignalClassifier.Exited:
                    return $"[PID={ProcessId}->{ExitCode}]/{Type}";

                case ProcessSignalClassifier.Output:
                case ProcessSignalClassifier.Error:
                    return $"[PID={ProcessId}]/{Type}: {Data}";
            }
            throw new NotImplementedException($"{nameof(ProcessSignalClassifier)}.{Type}");
        }
    }
}
