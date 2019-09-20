using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Observito.Diagnostics;
using Observito.Diagnostics.Types;
using System;

namespace ProcessObservableTest
{
    [TestClass]
    public class UnitTestTasks
    {
        private void GeneralInvariants(List<ProcessSignal> signals)
        {
            Assert.IsTrue(signals.StartsWith(__ => __.Type == ProcessSignalType.Started), $"Expected first signal classifier={ProcessSignalType.Started}");
            Assert.IsTrue(signals.FindLastIndex(s => s.Type == ProcessSignalType.OutputData) <= signals.FindLastIndex(s => s.Type == ProcessSignalType.OutputDataDone));
            Assert.IsTrue(signals.FindLastIndex(s => s.Type == ProcessSignalType.ErrorData) <= signals.FindLastIndex(s => s.Type == ProcessSignalType.ErrorDataDone));
            Assert.IsTrue(signals.EndsWith(__ => __.Type == ProcessSignalType.Exited || __.Type == ProcessSignalType.Disposed), $"Expected Last signal classifier={ProcessSignalType.Exited} or {ProcessSignalType.Disposed}");
        }

        [TestMethod]
        public async Task TaskOutputScenario()
        {
            const int n = 5;
            var result =  await 
                CreateProcessStartInfo
                .FromFile(Constants.IpsumExecutablePath, $"/output:{n}")
                .ToObservable()
                .RunAsync();
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
                Assert.IsTrue(l.Data == Constants.IpsumLines[c], $"Expected line data matches static lorem ipsum data");
                c++;
            }
        }

        [TestMethod]
        public async Task TaskErrorScenario()
        {
            const int n = 5;
            var result = await 
                CreateProcessStartInfo
                .FromFile(Constants.IpsumExecutablePath, $"/error:{n}")
                .EnsureFileExists()
                .ToObservable()
                .RunAsync(/*progress: new Progress<ProcessSignal>(p => Console.WriteLine(p.ToString()))*/);
            Assert.IsNotNull(result.ExitCode, "Expected exitcode");
            Assert.IsFalse(result.IsDisposed, "Unexpected dispose");
            Assert.IsNotNull(result.ProcessId, "Expected to find a process id");
            Assert.AreEqual(n, result.Data.Length, $"Expected to find {n} lines");
            Assert.IsTrue(result.Data.All(l => l.Type == DataLineType.Error), $"Not all lines are type={DataLineType.Error}");
            var c = 0;
            foreach (var l in result.Data)
            {
                Assert.AreEqual(DataLineType.Error, l.Type, $"Expected line type = error");
                Assert.AreEqual(c, l.LineNumber, $"Expected line number is sequential, starting with 0");
                c++;
            }
        }

        [TestMethod]
        public async Task TaskMixedScenario()
        {
            const int outputCount = 5;
            const int errorCount = 3;
            var result = await
                CreateProcessStartInfo
                .FromFile(Constants.IpsumExecutablePath, $"/output:{outputCount}", $"/error:{errorCount}")
                .ToObservable()
                .RunAsync();
            Assert.IsNotNull(result.ExitCode, "Expected exitcode");
            Assert.IsFalse(result.IsDisposed, "Unexpected dispose");
            Assert.IsNotNull(result.ProcessId, "Expected to find a process id");
            Assert.AreEqual((outputCount + errorCount), result.Data.Length, $"Expected to find {outputCount+outputCount} lines");
            Assert.IsTrue(result.Data.Count(l => l.Type == DataLineType.Output) == outputCount, $"Expected {outputCount} {DataLineType.Output} lines");
            Assert.IsTrue(result.Data.Count(l => l.Type == DataLineType.Error) == errorCount, $"Expected {errorCount} {DataLineType.Error} lines");
            var c = 0;
            foreach (var l in result.Data)
            {
                Assert.AreEqual(c, l.LineNumber, $"Expected line number is sequential, starting with 0");
                c++;
            }
        }

        [TestMethod]
        public async Task TaskScriptScenario()
        {
            const int outputCount = 10;
            const int errorCount = 5;
            var result = await
                CreateProcessStartInfo
                .FromFile(Constants.TestScriptFileName)
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

        /*
        [TestMethod]
        public async Task TaskFailureScenario()
        {
            var result = await ProcessObservable.FromExecutableFile("does not exist", "/fail").RunAsync();
            Assert.IsTrue(!result.IsDisposed, "Unexpected dispose");
            Assert.IsTrue(result.Data.Length == 0, $"Expected no output or error data");
            Assert.IsNotNull(result.Error, "Expected a failure");
        }
        */

        // TODO disposed test
        // TODO progress test
        // TODO failfast test
        // TODO cancellation token test
        // TODO cmd script parameter test
    }
}
