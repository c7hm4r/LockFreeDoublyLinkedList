#region license
// Copyright 2016 Christoph Müller
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Test.Tests;

namespace Test
{
    internal class Program
    {
        #region private
        private static void Main(string[] args)
        {
            Random rand = new Random();

            long remainingTests = 20000;
            var logIntervalStopwatch = new Stopwatch();
            var testBatchStopwatch = new Stopwatch();
            long testBatchSize;
            logIntervalStopwatch.Start();
            testBatchStopwatch.Start();
            while (true)
            {
                testBatchSize = remainingTests;
                var testRunsStopwatch = new Stopwatch();
                while (remainingTests > 0)
                {
                    if (logIntervalStopwatch.Elapsed.TotalSeconds >= 10)
                    {
                        Console.WriteLine("Number of remaining tests: "
                                        + remainingTests);
                        if (testBatchSize - remainingTests != 0)
                            Console.WriteLine("Remaining time: "
                                + (double)remainingTests
                                / (testBatchSize - remainingTests)
                                * testBatchStopwatch.Elapsed.TotalSeconds + " s");
                        logIntervalStopwatch.Restart();
                    }
                    remainingTests--;
                    var test = new Test001();
                    test.Seed = rand.Next(int.MinValue, int.MaxValue);
                    //Console.WriteLine("seed: {0}", test.Seed);

                    try
                    {
                        Thread.Yield();
                        testRunsStopwatch.Start();
                        Thread.MemoryBarrier();

                        test.Main(args);

                        Thread.MemoryBarrier();
                        testRunsStopwatch.Stop();
                    }
                    catch
                    {
                        Console.WriteLine($"The test failed. The seed was: {test.Seed}.");
                        throw;
                    }
                }
                if (testBatchSize > 0)
                {
                    Console.WriteLine($"Time per test: {testRunsStopwatch.Elapsed.TotalSeconds / testBatchSize} s.");
                }

                Console.Write("Enter 'r' or 'n', to restart the test. ?");
                string input = Console.ReadLine();
                Console.WriteLine();
                if (input == "r")
                    remainingTests = 1;
                else if (input == "n")
                {
                    Console.Write("Enter number of restarts. ?");
                    long i;
                    string text = Console.ReadLine();
                    if (long.TryParse(text, out i))
                    {
                        remainingTests = i;
                    }
                }
                if (remainingTests == 0)
                    break;
                testBatchStopwatch.Restart();
                logIntervalStopwatch.Restart();
            }
        }
        #endregion
    }

    internal abstract class Test
    {
        public abstract void Main(string[] args);
        public int Seed { get; set; }
    }
}
