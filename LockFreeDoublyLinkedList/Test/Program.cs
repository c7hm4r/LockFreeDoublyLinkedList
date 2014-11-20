using System;
using System.Threading;
using Test.Tests;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Random rand = new Random();

            int scheduledRepetitions = 1;
            var t2 = new HiPerfTimer.HpTimer();
            var t3 = new HiPerfTimer.HpTimer();
            long schedRepsOnT3Start = scheduledRepetitions;
            t2.Start();
            t3.Start();
            while (true)
            {
                if (scheduledRepetitions == 0)
                {
                    Console.Write("\'r' oder 'n' eingeben, um den Test neu zu starten. ?");
                    char key = Console.ReadKey().KeyChar;
                    Console.WriteLine();
                    if (key == 'r')
                        scheduledRepetitions = 1;
                    else if (key == 'n')
                    {
                        Console.Write("Anzahl der Neustarts eingeben. ?");
                        int i;
                        string text = Console.ReadLine();
                        if (int.TryParse(text, out i))
                        {
                            scheduledRepetitions = i;
                        }
                    }
                    if (scheduledRepetitions == 0)
                        break;
                    schedRepsOnT3Start = scheduledRepetitions;
                    t3.Start();
                    t2.Start();
                }
                if (t2.TimeSinceStart >= 10)
                {
                    Console.WriteLine("Number of remaining tests: "
                                      + scheduledRepetitions);
                    if (schedRepsOnT3Start - scheduledRepetitions != 0)
                        Console.WriteLine("Remaining time: "
                            + (double)scheduledRepetitions
                            / (schedRepsOnT3Start - scheduledRepetitions)
                            * t3.TimeSinceStart + " s");
                    t2.Start();
                }
                scheduledRepetitions--;
                var test = new Test001();
                test.Seed = rand.Next(int.MinValue, int.MaxValue);
                //Console.WriteLine("seed: {0}", test.Seed);
                var t = new HiPerfTimer.HpTimer();
                t.Start();
                Thread.MemoryBarrier();

                test.Main(args);

                Thread.MemoryBarrier();
                t.Stop();
                //Console.WriteLine("Dauer: " + t.Duration + " s.");
            }
        }
    }
    abstract class Test
    {
        public abstract void Main(string[] args);
        public int Seed { get; set; }
    }
}
