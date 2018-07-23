using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ObservableProcess;

namespace Tests
{
    [TestClass]
    public class UnitTest
    {
        private static readonly string[] LoremIpsumLines = new string[]{
            "At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis" ,
            "praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias" ,
            "excepturi sint occaecati cupiditate non provident, similique sunt in culpa qui" ,
            "officia deserunt mollitia animi, id est laborum et dolorum fuga.Et harum" ,
            "quidem rerum facilis est et expedita distinctio.Nam libero tempore, cum soluta" ,
            "nobis est eligendi optio cumque nihil impedit quo minus id quod maxime placeat" ,
            "facere possimus, omnis voluptas assumenda est, omnis dolor repellendus." ,
            "Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus" ,
            "saepe eveniet ut et voluptates repudiandae sint et molestiae non recusandae." ,
            "Itaque earum rerum hic tenetur a sapiente delectus, ut aut reiciendis" ,
            "voluptatibus maiores alias consequatur aut perferendis doloribus asperiores" ,
            "repellat."
        };

        [TestMethod]
        public void ObservableOutputScenario()
        {
            const int n = 5;
            var obs = ProcessObservable.FromFile("LoremIpsum.exe", $"/output:{n}");
            var results = obs.ToEnumerable().ToArray();

            Assert.IsTrue(results.StartsWith(__ => __.Type == ProcessSignalType.Started), $"Expected first signal classifier={ProcessSignalType.Started}");
            Assert.IsTrue(results.Any(__ => __.Type == ProcessSignalType.Exited && __.ExitCode == 0), "Expected at least one signal with exitcode=0");
            Assert.IsTrue(results.All(__ => __.Type != ProcessSignalType.Disposed), "Unexpected dispose");
            Assert.IsTrue(results.Any(__ => __.ProcessId != null), "Expected to find a process id");
            Assert.IsTrue(results.Count(__ => __.Type == ProcessSignalType.OutputData) == n, $"Expected to find {n} output lines");
            Assert.IsTrue(results.Count(__ => __.Type == ProcessSignalType.ErrorData) == 0, $"Expected to find 0 errors lines");
            Assert.IsTrue(results.EndsWith(__ => __.Type == ProcessSignalType.Exited || __.Type == ProcessSignalType.Disposed), $"Expected Last signal classifier={ProcessSignalType.Exited} or {ProcessSignalType.Disposed}");
            var c = 0;
            foreach (var result in results)
            {
                if (result.Type == ProcessSignalType.OutputData)
                {
                    Assert.IsTrue(result.Data == LoremIpsumLines[c], $"Expected line data matches static lorem ipsum data");
                    c++;
                }
            }
        }

        [TestMethod]
        public async Task TaskOutputScenario()
        {
            const int n = 5;
            var result = await ProcessObservable.FromFile("LoremIpsum.exe", $"/output:{n}").RunAsync();
            Assert.IsTrue(result.ExitCode == 0, "Expected exitcode=0");
            Assert.IsTrue(!result.IsDisposed, "Unexpected dispose");
            Assert.IsTrue(result.ProcessId != null, "Expected to find a process id");
            Assert.IsTrue(result.Data.Length == n, $"Expected to find {n} lines");
            Assert.IsTrue(result.Data.All(l => l.Type == DataLineType.Output), $"Not all lines are type={DataLineType.Output}");
            var c = 0;
            foreach (var l in result.Data)
            {
                Assert.IsTrue(l.Type == DataLineType.Output, $"Expected line type = output");
                Assert.IsTrue(l.LineNumber == c, $"Expected line number is sequential, starting with 0");
                Assert.IsTrue(l.Data == LoremIpsumLines[c], $"Expected line data matches static lorem ipsum data");
                c++;
            }
        }

        [TestMethod]
        public void ObservableErrorScenario()
        {
            const int n = 5;
            var obs = ProcessObservable.FromFile("LoremIpsum.exe", $"/error:{n}");
            var results = obs.ToEnumerable().ToArray();

            Assert.IsTrue(results.StartsWith(__ => __.Type == ProcessSignalType.Started), $"Expected first signal classifier={ProcessSignalType.Started}");
            Assert.IsTrue(results.Any(__ => __.Type == ProcessSignalType.Exited && __.ExitCode != null), "Expected at least one signal with exitcode");
            Assert.IsTrue(results.All(__ => __.Type != ProcessSignalType.Disposed), "Unexpected dispose");
            Assert.IsTrue(results.Any(__ => __.ProcessId != null), "Expected to find a process id");
            Assert.IsTrue(results.Count(__ => __.Type == ProcessSignalType.ErrorData) == n, $"Expected to find {n} error lines");
            Assert.IsTrue(results.Count(__ => __.Type == ProcessSignalType.OutputData) == 0, $"Expected to find 0 output lines");
            Assert.IsTrue(results.EndsWith(__ => __.Type == ProcessSignalType.Exited || __.Type == ProcessSignalType.Disposed), $"Expected Last signal classifier={ProcessSignalType.Exited} or {ProcessSignalType.Disposed}");
            var c = 0;
            foreach (var result in results)
            {
                if (result.Type == ProcessSignalType.OutputData)
                {
                    Assert.IsTrue(result.Data == LoremIpsumLines[c], $"Expected line data matches static lorem ipsum data");
                    c++;
                }
            }
        }

        [TestMethod]
        public async Task TaskErrorScenario()
        {
            const int n = 5;
            var result = await ProcessObservable.FromFile("LoremIpsum.exe", $"/error:{n}").RunAsync();
            Assert.IsTrue(result.ExitCode != null, "Expected exitcode");
            Assert.IsTrue(!result.IsDisposed, "Unexpected dispose");
            Assert.IsTrue(result.ProcessId != null, "Expected to find a process id");
            Assert.IsTrue(result.Data.Length == n, $"Expected to find {n} lines");
            Assert.IsTrue(result.Data.All(l => l.Type == DataLineType.Error), $"Not all lines are type={DataLineType.Error}");
            var c = 0;
            foreach (var l in result.Data)
            {
                Assert.IsTrue(l.Type == DataLineType.Error, $"Expected line type = error");
                Assert.IsTrue(l.LineNumber == c, $"Expected line number is sequential, starting with 0");
                c++;
            }
        }

        [TestMethod]
        public void ObservableMixedScenario()
        {
            const int errorCount = 5;
            const int outputCount = 10;
            var obs = ProcessObservable.FromFile("LoremIpsum.exe", $"/output:{errorCount} /error:{errorCount}");
            var results = obs.ToEnumerable().ToArray();

            Assert.IsTrue(results.StartsWith(__ => __.Type == ProcessSignalType.Started), $"Expected first signal classifier={ProcessSignalType.Started}");
            Assert.IsTrue(results.Any(__ => __.Type == ProcessSignalType.Exited && __.ExitCode != null), "Expected at least one signal with exitcode");
            Assert.IsTrue(results.All(__ => __.Type != ProcessSignalType.Disposed), "Unexpected dispose");
            Assert.IsTrue(results.Any(__ => __.ProcessId != null), "Expected to find a process id");
            Assert.IsTrue(results.Count(__ => __.Type == ProcessSignalType.ErrorData) == errorCount, $"Expected to find {errorCount} error lines");
            Assert.IsTrue(results.Count(__ => __.Type == ProcessSignalType.OutputData) == errorCount, $"Expected to find {outputCount} output lines");
            Assert.IsTrue(results.EndsWith(__ => __.Type == ProcessSignalType.Exited || __.Type == ProcessSignalType.Disposed), $"Expected Last signal classifier={ProcessSignalType.Exited} or {ProcessSignalType.Disposed}");
        }

        [TestMethod]
        public async Task TaskMixedScenario()
        {
            const int outputCount = 5;
            const int errorCount = 3;
            var result = await ProcessObservable.FromFile("LoremIpsum.exe", $"/output:{outputCount} /error:{errorCount}").RunAsync();
            Assert.IsTrue(result.ExitCode != null, "Expected exitcode");
            Assert.IsTrue(!result.IsDisposed, "Unexpected dispose");
            Assert.IsTrue(result.ProcessId != null, "Expected to find a process id");
            Assert.IsTrue(result.Data.Length == (outputCount + errorCount), $"Expected to find {outputCount+outputCount} lines");
            Assert.IsTrue(result.Data.Count(l => l.Type == DataLineType.Output) == outputCount, $"Expected {outputCount} {DataLineType.Output} lines");
            Assert.IsTrue(result.Data.Count(l => l.Type == DataLineType.Error) == errorCount, $"Expected {errorCount} {DataLineType.Error} lines");
            var c = 0;
            foreach (var l in result.Data)
            {
                Assert.IsTrue(l.LineNumber == c, $"Expected line number is sequential, starting with 0");
                c++;
            }
        }

        [TestMethod]
        public async Task TaskScriptScenario()
        {
            const int outputCount = 10;
            const int errorCount = 5;
            var result = await ProcessObservable.FromFile("TestScript.cmd").RunAsync();
            Assert.IsTrue(!result.IsDisposed, "Unexpected dispose");
            Assert.IsTrue(result.ProcessId != null, "Expected to find a process id");
            Assert.IsTrue(result.Data.Length >= (outputCount + errorCount), $"Expected at least {outputCount + outputCount} lines");
            Assert.IsTrue(result.Data.Count(l => l.Type == DataLineType.Output) >= outputCount, $"Expected at least {outputCount} {DataLineType.Output} lines");
            Assert.IsTrue(result.Data.Count(l => l.Type == DataLineType.Error) >= errorCount, $"Expected at least {errorCount} {DataLineType.Error} lines");
            var c = 0;
            foreach (var l in result.Data)
            {
                Assert.IsTrue(l.LineNumber == c, $"Expected line number is sequential, starting with 0");
                c++;
            }
        }

        [TestMethod]
        public async Task TaskFailureScenario()
        {
            var result = await ProcessObservable.FromExecutableFile("does not exist", "/fail").RunAsync();
            Assert.IsTrue(!result.IsDisposed, "Unexpected dispose");
            Assert.IsTrue(result.Data.Length == 0, $"Expected no output or error data");
            Assert.IsNotNull(result.Error, "Expected a failure");
        }

        // TODO disposed test
        // TODO progress test
        // TODO failfast test
        // TODO cancellation token test
        // TODO cmd script parameter test
    }

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
    }
}
