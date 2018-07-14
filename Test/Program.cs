using ObservableProcess;
using System;
using System.Diagnostics;
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

                var textColor = Console.ForegroundColor;

                // Convert to synchronous enumerable for simple demo purposes
                foreach (var signal in observeThisCmd.ToEnumerable())
                {
                    if (signal.Type == ProcessSignalClassifier.Error)
                        Console.ForegroundColor = ConsoleColor.Red;

                    // Dump the signal formatted text
                    Console.WriteLine(signal.ToString());

                    Console.ForegroundColor = textColor;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error running test: " + ex.Message);
            }

            Console.WriteLine("Pause");
            Console.ReadLine();
        }
    }
}
