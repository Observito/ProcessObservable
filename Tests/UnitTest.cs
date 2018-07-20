using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
            var obs = ProcessObservable.TryCreateFromFile("LoremIpsum.exe", $"/output:{n}");
            var results = obs.ToEnumerable().ToArray();

            Assert.IsTrue(results.Any(__ => __.Type == ProcessSignalClassifier.Exited && __.ExitCode == 0), "Expected at least one signal with exitcode=0");
            Assert.IsTrue(results.All(__ => __.Type != ProcessSignalClassifier.Disposed), "Unexpected dispose");
            Assert.IsTrue(results.Any(__ => __.ProcessId != null), "Expected to find a process id");
            Assert.IsTrue(results.Count(__ => __.Type == ProcessSignalClassifier.Output) == n, $"Expected to find {n} output lines");
            Assert.IsTrue(results.Count(__ => __.Type == ProcessSignalClassifier.Error) == 0, $"Expected to find 0 errors lines");
            var c = 0;
            foreach (var result in results)
            {
                if (result.Type == ProcessSignalClassifier.Output)
                {
                    Assert.IsTrue(result.Data == LoremIpsumLines[c], $"Expected line data matches static lorem ipsum data");
                    c++;
                }
            }
        }

        [TestMethod]
        public void TaskOutputScenario()
        {
            const int n = 5;
            var task = ProcessObservable.TryCreateFromFile("LoremIpsum.exe", $"/output:{n}").AsTask();
            task.Start();
            task.Wait();
            var result = task.Result;
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

        /*
            [TestMethod]
            public void TestMethod1()
            {

                        // Create from executable
                        var observeThisExe = ProcessObservable.TryCreateFromFile("LoremIpsum.exe", "/error:3");

                        // Create from script
                        var observeThisCmd = ProcessObservable.TryCreateFromFile(Path.Combine(Environment.CurrentDirectory, "TestScript.cmd"));

                        Console.WriteLine("First run -- the command line (observable #1)");
                        ConsoleWriteProcess(observeThisCmd);
                        Console.WriteLine("First run done");
                        Console.WriteLine();
                        Thread.Sleep(1000);

                        Console.WriteLine("Second run -- the executable file (observable #2)");
                        ConsoleWriteProcess(observeThisExe);
                        Console.WriteLine("Second run done");
                        Console.WriteLine();
                        Thread.Sleep(1000);

                        Console.WriteLine("Third run -- the executable file (observable #2 again)");
                        ConsoleWriteProcess(observeThisExe);
                        Console.WriteLine("Third run done");
                        Console.WriteLine();
                        Thread.Sleep(1000);
            }
         */
    }
}
