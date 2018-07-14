using System;
using System.Diagnostics;
using System.Threading;

namespace ProcessObservable.LoremIpsum
{
    public class Program
    {
        static void Main(string[] args)
        {
            var rnd = new Random();
            while (rnd.Next(0, 30) == 15)
            {
                Debug.WriteLine("Working...");

                if (rnd.Next(0, 30) == 20)
                    throw new Exception("Oops!");

                Thread.Sleep(rnd.Next(100, 750));
            }
            Debug.WriteLine("Completed ok");
        }
    }
}
