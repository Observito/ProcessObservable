namespace ObservableProcess
{
    /// <summary>
    /// Describes the kinds of signals a process can emit.
    /// </summary>
    public enum ProcessSignalClassifier
    {
        /// <summary>
        /// The source process emitted output data.
        /// </summary>
        Output,

        /// <summary>
        /// The source process emitted error data.
        /// </summary>
        Error,

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
