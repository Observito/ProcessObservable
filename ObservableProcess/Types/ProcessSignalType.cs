namespace ObservableProcess
{
    /// <summary>
    /// Describes the kinds of signals a process can emit.
    /// </summary>
    public enum ProcessSignalType
    {
        /// <summary>
        /// The process was started.
        /// Emits a process id.
        /// </summary>
        Started,

        /// <summary>
        /// The source process emitted output data.
        /// </summary>
        OutputData,

        /// <summary>
        /// The source process emitted error data.
        /// </summary>
        ErrorData,

        /// <summary>
        /// The source process emitted output data done.
        /// </summary>
        OutputDataDone,

        /// <summary>
        /// The source process emitted error data done.
        /// </summary>
        ErrorDataDone,

        /// <summary>
        /// The source process was exited.
        /// </summary>
        /// <remarks>The observable does not both emit an <see cref="Exited"/> and <see cref="Disposed"/> signal.</remarks>
        Exited,

        /// <summary>
        /// The source process was disposed.
        /// </summary>
        /// <remarks>The observable does not both emit an <see cref="Exited"/> and <see cref="Disposed"/> signal.</remarks>
        Disposed,
    }

    /// <summary>
    /// Process completion coordination type.
    /// </summary>
    internal struct ProcessCoordinationSignalType
    {
        /// <summary>
        /// The source process emitted output data done.
        /// </summary>
        public bool OutputDataDone;

        /// <summary>
        /// The source process emitted error data done.
        /// </summary>
        public bool ErrorDataDone;

        /// <summary>
        /// The source process was exited.
        /// </summary>
        public bool Exited;

        /// <summary>
        /// The source process was disposed.
        /// </summary>
        public bool Disposed;

        /// <summary>
        /// Coordination primitive to signal final completion (disposed or exited with no output/error pending).
        /// </summary>
        public bool Completed => OutputDataDone && ErrorDataDone && (Exited || Disposed);

        /// <summary>
        /// Convert signal type to coordination signal type.
        /// </summary>
        public static ProcessCoordinationSignalType Of(ProcessSignalType signalType)
        {
            return new ProcessCoordinationSignalType()
            {
                OutputDataDone = signalType == ProcessSignalType.OutputDataDone,
                ErrorDataDone = signalType == ProcessSignalType.ErrorDataDone,
                Exited = signalType == ProcessSignalType.Exited,
                Disposed = signalType == ProcessSignalType.Disposed,
            };
        }

        /// <summary>
        /// Compute sum of two coordination signal types.
        /// </summary>
        public static ProcessCoordinationSignalType operator +(ProcessCoordinationSignalType left, ProcessCoordinationSignalType right)
        {
            return new ProcessCoordinationSignalType()
            {
                OutputDataDone = left.OutputDataDone || right.OutputDataDone,
                ErrorDataDone = left.ErrorDataDone || right.ErrorDataDone,
                Exited = left.Exited || right.Exited,
                Disposed = left.Disposed || right.Disposed,
            };
        }
    }
}
