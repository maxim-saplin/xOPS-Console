using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Saplin.xOPS;

namespace xOPS_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var c = new Compute();
            int n = 10 * 1000 * 1000;

            Run(c, n, 1, inops: false, precision64Bit: false, useTasks: false);
            Run(c, n, 1, inops: false, precision64Bit: true, useTasks: false);
            Run(c, n, 1, inops: true, precision64Bit: false, useTasks: false);
            Run(c, n, 1, inops: true, precision64Bit: true, useTasks: false);

            int tpT, tpP;

            ThreadPool.GetMaxThreads(out tpT, out tpP);
            Console.WriteLine("\nCores: " + Environment.ProcessorCount + ";  pool threads: " + tpT + "; ports: " + tpP + "; timer freq: " + Stopwatch.Frequency);

            var threads = 4;

            Run(c, n, threads, inops: false, precision64Bit: false, useTasks: false);
            Run(c, n, threads, inops: false, precision64Bit: true, useTasks: false);
            Run(c, n, threads, inops: true, precision64Bit: false, useTasks: false);
            Run(c, n, threads, inops: true, precision64Bit: true, useTasks: false);

            threads = 8;

            Run(c, n, threads, inops: false, precision64Bit: false, useTasks: false);
            Run(c, n, threads, inops: false, precision64Bit: true, useTasks: false);
            Run(c, n, threads, inops: true, precision64Bit: false, useTasks: false);
            Run(c, n, threads, inops: true, precision64Bit: true, useTasks: false);

            threads = 32;

            Run(c, n, threads, inops: false, precision64Bit: false, useTasks: false);
            Run(c, n, threads, inops: false, precision64Bit: true, useTasks: false);
            Run(c, n, threads, inops: true, precision64Bit: false, useTasks: false);
            Run(c, n, threads, inops: true, precision64Bit: true, useTasks: false);

            //threads = 128;

            //Run(c, n, threads, inops: false, precision64Bit: false, useTasks: false);
            //Run(c, n, threads, inops: true, precision64Bit: false, useTasks: false);
            //Run(c, n, threads, inops: false, precision64Bit: true, useTasks: false);
            //Run(c, n, threads, inops: true, precision64Bit: true, useTasks: false);

            //threads = 256;

            //Run(c, n, threads, inops: false, precision64Bit: false, useTasks: false);
            //Run(c, n, threads, inops: true, precision64Bit: false, useTasks: false);
            //Run(c, n, threads, inops: false, precision64Bit: true, useTasks: false);
            //Run(c, n, threads, inops: true, precision64Bit: true, useTasks: false);

            Console.WriteLine("Press any key to Quit");
            Console.ReadKey();
        }

        private static void Run(Compute c, int n, int threads, bool inops, bool precision64Bit, bool useTasks)
        {
            const int repeats = 5;

            Double[] times = new Double[repeats];
            Double[] gxops = new Double[repeats];

            Console.WriteLine("\n{0}{1} tests in {2} {3}, N: {4}",
                inops ? "INT" : "FLT",
                precision64Bit ? "64" : "32",
                threads,
                useTasks ? "tasks" : "threads",
                n);

            Console.Write(inops ? "G_INOPS: " : "G_FLOPS: ");
            for (int i = 0; i < repeats; i++)
            {
                if (inops && threads == 1)
                {
                    if (precision64Bit) times[i] = c.RunInops64Bit(n); else times[i] = c.RunInops32Bit(n);
                    gxops[i] = (double)Compute.inopsPerIteration * n / times[i] / 1000000000;
                }
                else if (inops && threads > 1)
                {
                    times[i] = c.RunXopsMultiThreaded(n, threads, inops: true, precision64Bit: precision64Bit, useTasks: useTasks);
                    gxops[i] = (double)Compute.inopsPerIteration * n * threads / times[i] / 1000000000;
                }
                else if (!inops && threads == 1)
                {
                    if (precision64Bit) times[i] = c.RunFlops64Bit(n); else times[i] = c.RunFlops32Bit(n);
                    gxops[i] = (double)Compute.flopsPerIteration * n / times[i] / 1000000000;
                }
                else if (!inops && threads > 1)
                {
                    times[i] = c.RunXopsMultiThreaded(n, threads, inops: false, precision64Bit: precision64Bit, useTasks: useTasks);
                    gxops[i] = (double)Compute.flopsPerIteration * n * threads / times[i] / 1000000000;
                }

                Console.Write("{0:0.00}; ", gxops[i]);
            }

            Console.WriteLine("\n{1:0.00} {0}, {2:0.00}ms - averages", inops ? "G_INOPS" : "G_FLOPS" , gxops.Average(), times.Average()*1000);
        }

    }
}
