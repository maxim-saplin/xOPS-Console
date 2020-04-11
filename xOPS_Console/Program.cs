using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;
using Saplin.xOPS;

namespace xOPS_Console
{
    [DisassemblyDiagnoser(printSource: true, printInstructionAddresses:true, exportHtml: true, maxDepth: 4)]
    //[KeepBenchmarkFiles]
    //[Config(typeof(Config))]
    //[DryJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Mono)]
    //[DryJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net472)]
    [DryJob(BenchmarkDotNet.Jobs.RuntimeMoniker.NetCoreApp31)]
    public class Program
    {

        //    private class Config : ManualConfig
        //    {
        //        public Config()
        //        {
        //            Add(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
        //        }
        //    }

        //static void Main()
        //{
        //    //var config = DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator);
        //    //BenchmarkRunner.Run<Program>(config);
        //    BenchmarkRunner.Run<Program>();
        //}

        //[Benchmark]
        //public void RunFloat32()
        //{
        //    var res = c.RunXops(1000000000, false, false);
        //}

        [Benchmark]
        public void RunIntt32()
        {
            var res = c.RunXops(10000000, false, false);
        }

        static Compute c = new Compute();

        private static ManualResetEventSlim stressTestEnd;

        static void Main(string[] args)
        {
            int n = 50 * 1000 * 1000;

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


            Console.WriteLine("Press 'S' to start a Stress test, any other key to Quit");
            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.S)
            {
                Console.WriteLine("\nStarting Stress test on {0} threads... \nPress any key to stop\n", Environment.ProcessorCount * 2);

                Console.WriteLine("Duration:  (Warming up)");
                Console.WriteLine("Start:");
                Console.WriteLine("Now:");
                Console.WriteLine("Min:");
                Console.WriteLine("Max:");

                stressTestLinesToReturn = 5;

                stressTestEnd = new ManualResetEventSlim(false);

                var stressTest = new StressTest(Environment.ProcessorCount) { SamplingPeriodMs = 1000, WarmpUpSamples = 0 };
                stressTest.ResultsUpdated += StressTestUpdate;
                stressTest.Start();

                stressTestEnd.Wait();

                Console.WriteLine("\n Stress test ended");
            }
        }

        private static int stressTestLinesToReturn = 0;

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

        private static bool graphLinesUpdated = false;

        public static void StressTestUpdate(ContinuousRun sender)
        {
            if (!sender.WarmpingUp)
            {
                const string fs = "{0:0.00} GFLOPS \t\t{1:0.00} GINOPS";

                if (sender.TimeSeries[0].Results.Count < 2) return;

                var graphWidth = Console.WindowWidth / 2 - 2;

                var graph0 = AsciiTimeSeries.SeriesToLines(sender.TimeSeries[0].Results,
                    new AsciiOptions { HeigthLines = 10, MaxWidthCharacters = graphWidth, LabelFormat = "0.00" },
                    sender.TimeSeries[0].MinValue,
                    sender.TimeSeries[0].MaxValue);

                var graph1 = AsciiTimeSeries.SeriesToLines(sender.TimeSeries[1].Results,
                    new AsciiOptions { HeigthLines = 10, MaxWidthCharacters = graphWidth, LabelFormat = "0.00" },
                    sender.TimeSeries[1].MinValue,
                    sender.TimeSeries[1].MaxValue);

                var graphs = AsciiTimeSeries.MergeTwoGraphs(graph0, graph1, "  ", graphWidth);

                Console.CursorTop -= stressTestLinesToReturn;

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

                Console.WriteLine(graphs);

                if (!graphLinesUpdated)
                {
                    stressTestLinesToReturn += 10;
                    graphLinesUpdated = true;
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
