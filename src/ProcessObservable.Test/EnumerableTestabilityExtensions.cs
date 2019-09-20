using System;
using System.Collections.Generic;
using System.Linq;

namespace ProcessObservableTest
{
    /// <summary>
    /// Sequence testability extensions.
    /// Query methods that assist in testing the correctness of sequence generators.
    /// </summary>
    public static class EnumerableTestabilityExtensions
    {
        /// <summary>
        /// Does the sequence start with an element satisfying the predicate?
        /// If the sequence is empty the answer is no.
        /// </summary>
        public static bool StartsWith<T>(this IEnumerable<T> source, Func<T, bool> predicate) =>
            source.Any() && predicate(source.First());

        /// <summary>
        /// Does the sequence end with an element satisfying the predicate?
        /// If the sequence is empty the answer is no.
        /// </summary>
        public static bool EndsWith<T>(this IEnumerable<T> source, Func<T, bool> predicate) =>
            source.Any() && predicate(source.Last());

        public static bool OccursInSequence<T>(this IEnumerable<T> source, bool maySkip, bool mayRepeat, params Func<T, bool>[] tests)
        {
            int? done = null;
            foreach (var item in source)
            {
                for (var testIndex = 0; testIndex < tests.Length; testIndex++)
                {
                    if (tests[testIndex](item))
                    {
                        if (done == null)
                        {
                            if (testIndex > 0 && !maySkip)
                                return false;
                        }
                        else if (testIndex == done)
                        {
                            if (!mayRepeat)
                                return false;
                        }
                        else if (testIndex > done + 1)
                        {
                            return false;
                        }
                        done = testIndex;
                    }
                }
            }
            return true;
        }
    }
}
