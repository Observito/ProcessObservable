using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LoremIpsum
{
    class Program
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

        static int Main(string[] args)
        {
            if (args.Any(arg => string.Equals(arg, "/help", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"{nameof(LoremIpsumLines)} Syntax");
                Console.WriteLine();
                Console.WriteLine($" /help      show help");
                Console.WriteLine($" /args      show arguments");
                Console.WriteLine($" /rand      randomize output");
                Console.WriteLine($" /fail      throw exception");
                Console.WriteLine($" /exit:n    exit with given status code");
                Console.WriteLine($" /error:n   write n error lines");
                Console.WriteLine($" /output:n  write n output lines");
                return 0;
            }

            if (args.Any(arg => string.Equals(arg, "/args", StringComparison.OrdinalIgnoreCase)))
            {
                var maxIdx = $"{args.Length - 1}".Length;
                for (var i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    var index = $"{i}".PadLeft(maxIdx, ' ');
                    Console.WriteLine($"argument [#{index}; c={arg.Length} chars]: {arg}");
                }
            }

            if (args.Any(arg => string.Equals(arg, "/fail", StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception("Failure scenario triggered: /fail");
            }

            if (args.Any(arg => arg.ToLowerInvariant().StartsWith("/error:")))
            {
                var arg = args.First(__ => __.StartsWith("/error:"));
                var tuple = arg.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (tuple.Length < 2)
                {
                    Console.Error.WriteLine("error:n - `n´ not defined");
                    return -1000;
                }
                if (!int.TryParse(tuple[1], out int n))
                {
                    Console.Error.WriteLine("error:n - `n´ not an number");
                    return -1001;
                }
                if (n < 0)
                {
                    Console.Error.WriteLine("error:n - `n´ not a natural number");
                    return -1002;
                }
                for (var i = 0; i < n; i++)
                {
                    Console.Error.WriteLine(LoremIpsumLines[i]);
                }
            }

            if (args.Any(arg => arg.ToLowerInvariant().StartsWith("/output:")))
            {
                var arg = args.First(__ => __.StartsWith("/output:"));
                var tuple = arg.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (tuple.Length < 2)
                {
                    Console.Error.WriteLine("output:n - `n´ not defined");
                    return -1010;
                }
                if (!int.TryParse(tuple[1], out int n))
                {
                    Console.Out.WriteLine("output:n - `n´ not an number");
                    return -1011;
                }
                if (n < 0)
                {
                    Console.Out.WriteLine("output:n - `n´ not a natural number");
                    return -1012;
                }
                for (var i = 0; i < n; i++)
                {
                    Console.Out.WriteLine(LoremIpsumLines[i]);
                }
            }

            if (args.Any(x => x.Contains("rand")))
            {
                var rnd = new Random();
                var index = 0;
                while (rnd.Next(0, 30) != 15)
                {
                    Debug.WriteLine("Working...");

                    Console.WriteLine(LoremIpsumLines[index]);

                    var err = rnd.Next(0, 30);
                    if (err >= 20 && err <= 23)
                        Console.Error.WriteLine("Random error!");

                    if (err == 1)
                        throw new Exception("Random failure!");

                    Thread.Sleep(rnd.Next(100, 350));

                    index = (index + 1) % LoremIpsumLines.Length;
                }
                Debug.WriteLine("Completed");
                return rnd.Next(-100, 100);
            }

            return 0;
        }
    }
}
