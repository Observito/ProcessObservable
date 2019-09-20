using System;

namespace Observito.Diagnostics.Types
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
        public ProcessSignalType Type { get; private set; }

        /// <summary>
        /// The id of the specific process that this signal is observed from.
        /// </summary>
        public int? ProcessId { get; private set; }

        /// <summary>
        /// Optional data from the process. 
        /// Relevant for <see cref="ProcessSignalType.OutputData"/> and <see cref="ProcessSignalType.ErrorData"/>.
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        /// If the process has exited, then this is the process exit code.
        /// Relevant for <see cref="ProcessSignalType.Exited"/>
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
                case ProcessSignalType.Started:
                case ProcessSignalType.OutputDataDone:
                case ProcessSignalType.ErrorDataDone:
                case ProcessSignalType.Disposed:
                    return $"[PID={ProcessId}]/{Type}";

                case ProcessSignalType.OutputData:
                case ProcessSignalType.ErrorData:
                    return $"[PID={ProcessId}]/{Type}: {Data}";

                case ProcessSignalType.Exited:
                    return $"[PID={ProcessId}->{ExitCode}]/{Type}";
            }
            throw new NotImplementedException($"{nameof(ProcessSignalType)}.{Type}");
        }

        #region Scenario constructors
        /// <summary>
        /// Create a <see cref="ProcessSignalType.Started"/> signal.
        /// </summary>
        public static ProcessSignal FromStarted(int processId) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalType.Started;
                __.ProcessId = processId;
            });

        /// <summary>
        /// Create a <see cref="ProcessSignalType.OutputData"/> signal.
        /// </summary>
        public static ProcessSignal FromOutputData(int processId, string data) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalType.OutputData;
                __.ProcessId = processId;
                __.Data = data;
            });

        /// <summary>
        /// Create a <see cref="ProcessSignalType.ErrorData"/> signal.
        /// </summary>
        public static ProcessSignal FromErrorData(int? processId, string data) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalType.ErrorData;
                __.ProcessId = processId;
                __.Data = data;
            });

        /// <summary>
        /// Create a <see cref="ProcessSignalType.OutputDataDone"/> signal.
        /// </summary>
        public static ProcessSignal FromOutputDataDone(int processId) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalType.OutputDataDone;
                __.ProcessId = processId;
            });

        /// <summary>
        /// Create a <see cref="ProcessSignalType.ErrorDataDone"/> signal.
        /// </summary>
        public static ProcessSignal FromErrorDataDone(int? processId) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalType.ErrorDataDone;
                __.ProcessId = processId;
            });


        /// <summary>
        /// Create a <see cref="ProcessSignalType.Exited"/> signal.
        /// </summary>
        public static ProcessSignal FromExited(int processId, int exitCode) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalType.Exited;
                __.ProcessId = processId;
                __.ExitCode = exitCode;
            });

        /// <summary>
        /// Create a <see cref="ProcessSignalType.Disposed"/> signal.
        /// </summary>
        public static ProcessSignal FromDisposed(int processId) =>
            new ProcessSignal(__ => {
                __.Type = ProcessSignalType.Disposed;
                __.ProcessId = processId;
            });
        #endregion
    }
}
