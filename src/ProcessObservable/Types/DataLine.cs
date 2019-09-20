using System;

namespace Observito.Diagnostics.Types
{
    /// <summary>
    /// A single piece of line data.
    /// </summary>
    public class DataLine
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DataLine(DataLineType type, string data, DateTimeOffset instant, int lineNumber)
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
        public DateTimeOffset Instant { get; }

        /// <summary>
        /// Line number.
        /// </summary>
        public int LineNumber { get; }

        public override string ToString() =>
            $"#{LineNumber}@{Instant:HH:mm:ss.fffff}/{Type}: {Data}";
    }
}
