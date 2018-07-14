using System;
using System.Diagnostics;
using System.Threading;
namespace LoremIpsum
{
    class Program
    {
        private static readonly string[] LoremIpsum = new string[]{
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

        static void Main(string[] args)
        {
            var rnd = new Random();
            var index = 0;
            while (rnd.Next(0, 30) != 15)
            {
                Debug.WriteLine("Working...");

                Console.WriteLine(LoremIpsum[index]);

                var err = rnd.Next(0, 30);
                if (err >= 20 && err <= 23)
                {
                    throw new Exception("Oops!");
                }

                Thread.Sleep(rnd.Next(100, 750));

                index = (index + 1) % LoremIpsum.Length;
            }
            Debug.WriteLine("Completed ok");
        }

    }
}
