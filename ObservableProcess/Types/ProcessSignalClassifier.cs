﻿namespace ObservableProcess
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
}
