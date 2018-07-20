using System;

namespace ObservableProcess
{
    /// <summary>
    /// A single piece of line data.
    /// </summary>
    public class DataLine
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DataLine(DataLineType type, string data, DateTime instant, int lineNumber)
        {
            Type = type;
            Data = data;
            Instant = instant;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// The type of line data.
        /// </summary>
        public DataLineType Type { get; }

        /// <summary>
        /// The line data.
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// When did the data occur?
        /// </summary>
        public DateTime Instant { get; }

        /// <summary>
        /// Line number.
        /// </summary>
        public int LineNumber { get; }

        public override string ToString() =>
            $"#{LineNumber}@{Instant:HH:mm:ss.fff}/{Type}: {Data}";
    }
}
