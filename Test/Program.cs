using ObservableProcess;
using System;
using System.IO;
using System.Reactive.Linq;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Create from executable
                var observeThisExe = ProcessObservable.TryCreateFromFile("LoremIpsum.exe");

                // Create from script
                var observeThisCmd = ProcessObservable.TryCreateFromFile(Path.Combine(Environment.CurrentDirectory, "TestScript.cmd"));

                Console.WriteLine("First run -- the command line (observable #1)");
                ConsoleWriteProcess(observeThisCmd);

                Console.WriteLine("Second run -- the executable file (observable #2)");
                ConsoleWriteProcess(observeThisExe);

                Console.WriteLine("Third run -- the executable file (observable #2 again)");
                ConsoleWriteProcess(observeThisExe);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error running test: " + ex.Message);
            }

            Console.WriteLine("Pause");
            Console.ReadLine();
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
