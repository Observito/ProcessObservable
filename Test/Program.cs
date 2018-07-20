using ObservableProcess;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO test cases

            /*
            REM LoremIpsum.exe /rand
            LoremIpsum.exe / help
            LoremIpsum.exe / args
            LoremIpsum.exe / output
            LoremIpsum.exe / error
            LoremIpsum.exe / fail
            */

            try
            {
                /*
                foreach (var item in ProcessObservable.TryCreateFromFile("LoremIpsum.exe", "/output:5").ToEnumerable())
                {
                    Console.WriteLine(item);
                }
                Console.ReadLine();
                return;
                */

                Console.WriteLine("Starting task");
                var task = ProcessObservable.TryCreateFromFile("LoremIpsum.exe", "/output:5").AsTask();
                task.Start();
                task.Wait();
                Console.WriteLine(task.Result);
                Console.WriteLine("Task done");
                Console.WriteLine();
                Thread.Sleep(1000);

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
            catch (Exception ex)
            {
                Console.WriteLine("Error running test: " + ex.Message);
            }

            Console.WriteLine("Pause");
            Console.ReadLine();

            return;
        }

        private static void ConsoleWriteProcess(IObservable<ProcessSignal> op)
        {
            var textColor = Console.ForegroundColor;

            foreach (var signal in op.ToEnumerable())
            {
                if (signal.Type == ProcessSignalClassifier.Error)
                    Console.ForegroundColor = ConsoleColor.Red;

                // Dump the signal formatted text
                Console.WriteLine(signal.ToString());

                Console.ForegroundColor = textColor;
            }
        }
    }
}
