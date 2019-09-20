using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Observito.Diagnostics;
using Observito.Diagnostics.Types;

namespace ProcessObservableTest
{
    [TestClass]
    public class UnitTestObservables
    {
        private void GeneralInvariants(List<ProcessSignal> signals)
        {
            Assert.IsTrue(signals.StartsWith(__ => __.Type == ProcessSignalType.Started), $"Expected first signal classifier={ProcessSignalType.Started}");
            Assert.IsTrue(signals.FindLastIndex(s => s.Type == ProcessSignalType.OutputData) <= signals.FindLastIndex(s => s.Type == ProcessSignalType.OutputDataDone));
            Assert.IsTrue(signals.FindLastIndex(s => s.Type == ProcessSignalType.ErrorData) <= signals.FindLastIndex(s => s.Type == ProcessSignalType.ErrorDataDone));
            Assert.IsTrue(signals.EndsWith(__ => __.Type == ProcessSignalType.Exited || __.Type == ProcessSignalType.Disposed), $"Expected Last signal classifier={ProcessSignalType.Exited} or {ProcessSignalType.Disposed}");
        }

        [TestMethod]
        public void ObservableOutputScenario()
        {
            const int n = 5;
            var obs = 
                CreateProcessStartInfo
                .FromFile(Constants.IpsumExecutablePath, $"/output:{n}")
                .ToObservable();
            var signals = obs.ToEnumerable().ToList();

            GeneralInvariants(signals);
            Assert.IsTrue(signals.Any(__ => __.Type == ProcessSignalType.Exited && __.ExitCode == 0), "Expected at least one signal with exitcode=0");
            Assert.IsTrue(signals.All(__ => __.Type != ProcessSignalType.Disposed), "Unexpected dispose");
            Assert.IsTrue(signals.Any(__ => __.ProcessId != null), "Expected to find a process id");
            Assert.IsTrue(signals.Count(__ => __.Type == ProcessSignalType.OutputData) == n, $"Expected to find {n} output lines");
            Assert.IsTrue(signals.Count(__ => __.Type == ProcessSignalType.ErrorData) == 0, $"Expected to find 0 errors lines");
            var c = 0;
            foreach (var result in signals)
            {
                if (result.Type == ProcessSignalType.OutputData)
                {
                    Assert.IsTrue(result.Data == Constants.IpsumLines[c], $"Expected line data matches static lorem ipsum data");
                    c++;
                }
            }
        }
        
        [TestMethod]
        public void ObservableErrorScenario()
        {
            const int n = 5;
            var obs = CreateProcessStartInfo
                .FromFile(Constants.IpsumExecutablePath, $"/error:{n}")
                .ToObservable();
            var signals = obs.ToEnumerable().ToList();

            GeneralInvariants(signals);
            Assert.IsTrue(signals.Any(__ => __.Type == ProcessSignalType.Exited && __.ExitCode != null), "Expected at least one signal with exitcode");
            Assert.IsTrue(signals.All(__ => __.Type != ProcessSignalType.Disposed), "Unexpected dispose");
            Assert.IsTrue(signals.Any(__ => __.ProcessId != null), "Expected to find a process id");
            Assert.IsTrue(signals.Count(__ => __.Type == ProcessSignalType.ErrorData) == n, $"Expected to find {n} error lines");
            Assert.IsTrue(signals.Count(__ => __.Type == ProcessSignalType.OutputData) == 0, $"Expected to find 0 output lines");
            var c = 0;
            foreach (var result in signals)
            {
                if (result.Type == ProcessSignalType.OutputData)
                {
                    Assert.AreEqual(Constants.IpsumLines[c], result.Data, $"Expected line data matches static lorem ipsum data");
                    c++;
                }
            }
        }
        
        [TestMethod]
        public void ObservableMixedScenario()
        {
            const int errorCount = 5;
            const int outputCount = 10;
            var obs = 
                CreateProcessStartInfo
                .FromFile(Constants.IpsumExecutablePath, $"/output:{errorCount}", $"/error:{errorCount}")
                .EnsureFileExists()
                .ToObservable();
            var signals = obs.ToEnumerable().ToList();

            GeneralInvariants(signals);
            Assert.IsTrue(signals.Any(__ => __.Type == ProcessSignalType.Exited && __.ExitCode != null), "Expected at least one signal with exitcode");
            Assert.IsTrue(signals.All(__ => __.Type != ProcessSignalType.Disposed), "Unexpected dispose");
            Assert.IsTrue(signals.Any(__ => __.ProcessId != null), "Expected to find a process id");
            Assert.IsTrue(signals.Count(__ => __.Type == ProcessSignalType.ErrorData) == errorCount, $"Expected to find {errorCount} error lines");
            Assert.IsTrue(signals.Count(__ => __.Type == ProcessSignalType.OutputData) == errorCount, $"Expected to find {outputCount} output lines");
        }
        
        [TestMethod]
        public async Task TaskScriptScenario()
        {
            const int outputCount = 10;
            const int errorCount = 5;
            var result =
                    await CreateProcessStartInfo
                    .FromFile(Constants.TestScriptFileName)
                    .EnsureFileExists()
                    .ToObservable()
                    .RunAsync();
            Assert.IsFalse(result.IsDisposed, "Unexpected dispose");
            Assert.IsNotNull(result.ProcessId, "Expected to find a process id");
            Assert.IsTrue(result.Data.Length >= (outputCount + errorCount), $"Expected at least {outputCount + outputCount} lines");
            Assert.IsTrue(result.Data.Count(l => l.Type == DataLineType.Output) >= outputCount, $"Expected at least {outputCount} {DataLineType.Output} lines");
            Assert.IsTrue(result.Data.Count(l => l.Type == DataLineType.Error) >= errorCount, $"Expected at least {errorCount} {DataLineType.Error} lines");
            var c = 0;
            foreach (var l in result.Data)
            {
                Assert.AreEqual(c, l.LineNumber, $"Expected line number is sequential, starting with 0");
                c++;
            }
        }

        [TestMethod]
        public void ManyObservableOutputScenario()
        {
            const int repetitions = 30;
            const int n = 5;
            var obs = 
                CreateProcessStartInfo
                .FromFile(Constants.IpsumExecutablePath, $"/output:{n}")
                .EnsureFileExists()
                .ToObservable();
            foreach (var _ in Enumerable.Range(0, repetitions))
            {
                var signals = obs.ToEnumerable().ToList();

                GeneralInvariants(signals);
                Assert.IsTrue(signals.Any(__ => __.Type == ProcessSignalType.Exited && __.ExitCode == 0), "Expected at least one signal with exitcode=0");
                Assert.IsTrue(signals.All(__ => __.Type != ProcessSignalType.Disposed), "Unexpected dispose");
                Assert.IsTrue(signals.Any(__ => __.ProcessId != null), "Expected to find a process id");
                Assert.IsTrue(signals.Count(__ => __.Type == ProcessSignalType.OutputData) == n, $"Expected to find {n} output lines");
                Assert.IsTrue(signals.Count(__ => __.Type == ProcessSignalType.ErrorData) == 0, $"Expected to find 0 errors lines");
                var c = 0;
                foreach (var result in signals)
                {
                    if (result.Type == ProcessSignalType.OutputData)
                    {
                        Assert.AreEqual(Constants.IpsumLines[c], result.Data, $"Expected line data matches static lorem ipsum data");
                        c++;
                    }
                }
            }
        }

        // TODO disposed test
        // TODO progress test
        // TODO failfast test
        // TODO cancellation token test
        // TODO cmd script parameter test
    }
}
