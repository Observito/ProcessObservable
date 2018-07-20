using System;

namespace ObservableProcess
{
    /// <summary>
    /// A signal from a process observable. 
    /// Signals carry all captured observable side-effects from a process.
    /// </summary>
    public class ProcessSignal
    {
        private ProcessSignal(Action<ProcessSignal> initializer)
        {
            initializer(this);
        }

        /// <summary>
        /// Classification of the signal type.
        /// </summary>
        public ProcessSignalClassifier Type { get; private set; }

        /// <summary>
        /// The id of the specific process that this signal is observed from.
        /// </summary>
        public int? ProcessId { get; private set; }

        /// <summary>
        /// Optional data from the process. 
        /// Relevant for <see cref="ProcessSignalClassifier.Output"/> and <see cref="ProcessSignalClassifier.Error"/>.
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        /// If the process has exited, then this is the process exit code.
        /// Relevant for <see cref="ProcessSignalClassifier.Exited"/>
        /// </summary>
        public int? ExitCode { get; private set; }

        /// <summary>
        /// A formatted version of process signal.
        /// </summary>
        /// <returns>The formatted string.</returns>
        public override string ToString()
        {
            switch (Type)
            {
                case ProcessSignalClassifier.Started:
                    return $"[PID={ProcessId}]/{Type}";

                case ProcessSignalClassifier.Output:
                case ProcessSignalClassifier.Error:
                    return $"[PID={ProcessId}]/{Type}: {Data}";

                case ProcessSignalClassifier.Exited:
                    return $"[PID={ProcessId}->{ExitCode}]/{Type}";

                case ProcessSignalClassifier.Disposed:
                    return $"[PID={ProcessId}]/{Type}";
            }
            throw new NotImplementedException($"{nameof(ProcessSignalClassifier)}.{Type}");
        }

        #region Scenario constructors
        /// <summary>
        /// Create a <see cref="ProcessSignalClassifier.Started"/> signal.
        /// </summary>
        public static ProcessSignal FromStarted(int processId) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalClassifier.Started;
                __.ProcessId = processId;
            });

        /// <summary>
        /// Create a <see cref="ProcessSignalClassifier.Output"/> signal.
        /// </summary>
        public static ProcessSignal FromOutput(int processId, string data) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalClassifier.Output;
                __.ProcessId = processId;
                __.Data = data;
            });

        /// <summary>
        /// Create a <see cref="ProcessSignalClassifier.Error"/> signal.
        /// </summary>
        public static ProcessSignal FromError(int? processId, string data) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalClassifier.Error;
                __.ProcessId = processId;
                __.Data = data;
            });

        /// <summary>
        /// Create a <see cref="ProcessSignalClassifier.Exited"/> signal.
        /// </summary>
        public static ProcessSignal FromExited(int processId, int exitCode) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalClassifier.Exited;
                __.ProcessId = processId;
                __.ExitCode = exitCode;
            });

        /// <summary>
        /// Create a <see cref="ProcessSignalClassifier.Disposed"/> signal.
        /// </summary>
        public static ProcessSignal FromDisposed(int processId) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalClassifier.Disposed;
                __.ProcessId = processId;
            });
        #endregion
    }
}
