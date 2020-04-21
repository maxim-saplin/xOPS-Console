using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Saplin.TimeSeries;
using Saplin.xOPS;

namespace xOPS_Console
{
    public class Program
    {
        static Compute c = new Compute();

        private static ManualResetEventSlim stressTestEnd;

        static void Main(string[] args)
        {
            int n = 50 * 1000 * 1000;

            Console.Clear();
            Console.WriteLine("xOPS CPU Benchmark");

            Run(c, n, 1, inops: false, precision64Bit: false, useTasks: false);
            Run(c, n, 1, inops: true, precision64Bit: false, useTasks: false);
            //Run(c, n, 1, inops: false, precision64Bit: true, useTasks: false);
            //Run(c, n, 1, inops: true, precision64Bit: true, useTasks: false);

            int tpT, tpP;

            ThreadPool.GetMaxThreads(out tpT, out tpP);
            Console.WriteLine("\nCores: " + Environment.ProcessorCount + "; timer freq: " + Stopwatch.Frequency);

            var threads = Environment.ProcessorCount * 2;

            Run(c, n, threads, inops: false, precision64Bit: false, useTasks: false);
            Run(c, n, threads, inops: true, precision64Bit: false, useTasks: false);
            //Run(c, n, threads, inops: false, precision64Bit: true, useTasks: false);
            //Run(c, n, threads, inops: true, precision64Bit: true, useTasks: false);


            Console.WriteLine("\nWARNING! Stress Test may lead to CPU overheating and damages. \n" +
                "If you're not sure of your hardware - DO NOT PROCEED!\n" +
                "Press 'S' to start a Stress test, any other key to Quit");
            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.S)
            {
                Console.WriteLine("\nStress testing on {0} threads... Press any key to stop\n", Environment.ProcessorCount * 2);

                Console.WriteLine("Duration:  (Warming up)");
                Console.WriteLine("Start:");
                Console.WriteLine("Min:");
                Console.WriteLine("Max:");
                Console.WriteLine("Now:");

                stressTestLinesCursorTopDiff = 5;

                stressTestEnd = new ManualResetEventSlim(false);

                var stressTest = new StressTest(Environment.ProcessorCount) { SamplingPeriodMs = 1000, WarmpUpSamples = 0 };
                stressTest.ResultsUpdated += StressTestUpdate;
                stressTest.Start();

                stressTestEnd.Wait();

                Console.WriteLine("\n Stress test ended");
            }
        }

        static int stressTestLinesCursorTopDiff = 0;
        static int stressTestWindowHeight = 0;

        private static void Run(Compute c, int n, int threads, bool inops, bool precision64Bit, bool useTasks)
        {
            const int repeats = 3;

            Double[] times = new Double[repeats];
            Double[] gxops = new Double[repeats];

            Console.WriteLine("\n{0}{1} tests in {2} {3}, N: {4}",
                inops ? "INT" : "FLT",
                precision64Bit ? "64" : "32",
                threads,
                useTasks ? "task(s)" : "thread(s)",
                n);

            Console.Write(inops ? "G_INOPS: " : "G_FLOPS: ");
            for (int i = 0; i < repeats; i++)
            {
                if (inops && threads == 1)
                {
                    times[i] = c.RunXops(n, inops, precision64Bit);
                    gxops[i] = c.LastResultSTGigaOPSAveraged;
                    //gxops[i] = (double)Compute.inopsPerIteration * n / times[i] / 1000000000;
                }
                else if (inops && threads > 1)
                {
                    times[i] = c.RunXopsMultiThreaded(n, threads, inops: true, precision64Bit: precision64Bit, useTasks: useTasks);
                    gxops[i] = (double)Compute.inopsPerIteration * n * threads / times[i] / 1000000000;
                }
                else if (!inops && threads == 1)
                {
                    times[i] = c.RunXops(n, inops, precision64Bit);
                    gxops[i] = c.LastResultSTGigaOPSAveraged;
                    //gxops[i] = (double)Compute.flopsPerIteration * n / times[i] / 1000000000;
                }
                else if (!inops && threads > 1)
                {
                    times[i] = c.RunXopsMultiThreaded(n, threads, inops: false, precision64Bit: precision64Bit, useTasks: useTasks);
                    gxops[i] = (double)Compute.flopsPerIteration * n * threads / times[i] / 1000000000;
                }

                Console.Write("{0:0.00}; ", gxops[i]);
            }

            Console.WriteLine("\n{1:0.00} {0}, {2:0.00}ms - averages", inops ? "G_INOPS" : "G_FLOPS", gxops.Average(), times.Average() * 1000);
        }

        static AsciiTimeSeries flopsGraph, inopsGraph;


        public static void StressTestUpdate(ContinuousRun sender)
        {
            if (!sender.WarmpingUp)
            {
                const string fs = "{0:0.00} GFLOPS \t\t{1:0.00} GINOPS";

                if (sender.TimeSeries[0].Results.Count < 2) return;

                lock (sender)
                {
                    Console.CursorTop -= stressTestLinesCursorTopDiff;
                    Debug.WriteLine("Before Text, Console.CursorTop: " + Console.CursorTop);

                    Console.CursorLeft = 10;
                    Console.WriteLine(sender.Elapsed.ToString());
                    Console.CursorLeft = 10;
                    Console.WriteLine(fs, sender.TimeSeries[0].StartValue, sender.TimeSeries[1].StartValue);
                    Console.CursorLeft = 10;
                    Console.WriteLine(fs, sender.TimeSeries[0].MinValue, sender.TimeSeries[1].MinValue);
                    Console.CursorLeft = 10;
                    Console.WriteLine(fs, sender.TimeSeries[0].MaxValue, sender.TimeSeries[1].MaxValue);
                    Console.CursorLeft = 10;
                    Console.WriteLine(fs, sender.TimeSeries[0].CurrentValue, sender.TimeSeries[1].CurrentValue);

                    if (flopsGraph == null)
                    {
                        var graphWidth = Console.WindowWidth / 2 - 1;
                        var graphHeight = 10;

                        for (int i = 0; i < graphHeight; i++)
                            Console.WriteLine();

                        stressTestLinesCursorTopDiff += graphHeight -1;

                        Console.CursorTop -= graphHeight;

                        flopsGraph = new AsciiTimeSeries()
                        { HeigthLines = graphHeight, WidthCharacters = graphWidth, LabelFormat = "0.00", AbovePointChar = '·', EmptyChar = '|', YAxisAndLabelsWidth = 7, YLabelRightPadding = 0 };
                        inopsGraph = new AsciiTimeSeries()
                        { HeigthLines = graphHeight, WidthCharacters = graphWidth, LabelFormat = "0.00", AbovePointChar = '·', EmptyChar = '|', YAxisAndLabelsWidth = 7, YLabelRightPadding = 0 };

                        flopsGraph.Series = sender.TimeSeries[0].Results;
                        inopsGraph.Series = sender.TimeSeries[1].Results;
                    }

                    flopsGraph.Min = 0;//sender.TimeSeries[0].MinValue;
                    flopsGraph.Max = sender.TimeSeries[0].MaxValue;

                    inopsGraph.Min = 0;// sender.TimeSeries[1].MinValue;
                    inopsGraph.Max = sender.TimeSeries[1].MaxValue;

                    var graphs = AsciiTimeSeries.MergeTwoGraphs(flopsGraph, inopsGraph, "#");

                    Console.Write(graphs);
                }
            }

            if (Console.KeyAvailable)
            {
                sender.Stop();
                stressTestEnd.Set();
            }
        }

    }
}