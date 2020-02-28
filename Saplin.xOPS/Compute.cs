using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Saplin.xOPS
{
    /// <summary>
    /// Contains a number of methods accepting the number of iterations and returning ammount of seconds it took to complete the operations
    /// </summary>
    public class Compute
    {
        public const int flopsPerIteration = 30; // to be determined based on IL disassembly
        public const int inopsPerIteration = 34;  // to be determined based on IL disassembly

        protected Single prevSingleY;
        protected Double prevDoubleY;
        protected Int32 prevInt32Y;
        protected Int64 prevInt64Y;

        private Stopwatch sw = new Stopwatch();

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public Double RunFlops32Bit(int iterations)
        {
            // Single precision has 23 bit mantise which in normilized form gives 24 significant bits, i.e. ~16,7m values.
            // The main loop uses Single as a counter and it will stop gorwing after 16.7m iterations as mantisa won't have the precision and counter will stall, endless loop will happen
            if (iterations > 16 * 1000 * 1000) 
                throw new ArgumentOutOfRangeException("For single precision float calculations the number of iterations can't be more than 16 millions");

            sw.Restart();

            Single counter = 0, increment = 1, max = iterations;
            Single startValue = -(Single)Math.PI, endValue = (Single)Math.PI, x = startValue, x2, y = 0, pi2 = (Single)(Math.PI*Math.PI);
            Single funcInc = (endValue - startValue) / iterations;

            // Changes to the body of the loop must be refelected in flopsPerIteration const
            while (counter < max)
            {
                counter += increment;

                x2 = x * x;
                y = (pi2 - 4 * x2);
                y /= (pi2 + x2);
                //y = Math.Abs(y);
                x += funcInc;
            }

            prevDoubleY = y;

            sw.Stop();

            var time = ((Double)sw.ElapsedTicks) / Stopwatch.Frequency;

            LastResultGigaOPS = TimeToGigaOPS(time, iterations, 1, inops: false);

            return time;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public Double RunFlops64Bit(int iterations)
        {
            sw.Restart();

            Double counter = 0, increment = 1, max = iterations;
            Double startValue = -(Double)Math.PI, endValue = (Double)Math.PI, x = startValue, x2, y = 0, pi2 = (Double)(Math.PI * Math.PI);
            Double funcInc = (endValue - startValue) / iterations;

            // Changes to the body of the loop must be refelected in flopsPerIteration const
            while (counter < max)
            {
                counter += increment;

                x2 = x * x;
                y = (pi2 - 4 * x2);
                y /= (pi2 + x2);
                //y = Math.Abs(y);
                x += funcInc;
            }

            prevDoubleY = y;

            sw.Stop();

            var time = ((Double)sw.ElapsedTicks) / Stopwatch.Frequency;

            LastResultGigaOPS = TimeToGigaOPS(time, iterations, 1, inops: false);

            return time;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public Double RunInops32Bit(int iterations)
        {
            sw.Restart();

            Int32 counter = 0, increment = 1, max = iterations;
            Int32 x = Int32.MinValue, x2, y = 0, coef = 3;
            Int32 funcInc = (Int32)((UInt32.MaxValue) / (Int32)iterations);

            // Changes to the body of the loop must be refelected in inopsPerIteration const
            while (counter < max)
            {
                counter += increment;

                x2 = x/2;
                y = (coef - 4 * x2);
                y /= (coef + x2);
                y -= coef;
                //y = Math.Abs(y);
                x += funcInc;
            }

            prevInt32Y = y;

            sw.Stop();

            var time = ((Double)sw.ElapsedTicks) / Stopwatch.Frequency;

            LastResultGigaOPS = TimeToGigaOPS(time, iterations, 1, inops: true);

            return time;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public Double RunInops64Bit(int iterations)
        {
            sw.Restart();

            Int64 counter = 0, increment = 1, max = iterations;
            Int64 x = Int64.MinValue, x2, y = 0, coef = 3;
            Int64 funcInc = (Int64)(UInt64.MaxValue / (UInt64)iterations);

            // Changes to the body of the loop must be refelected in inopsPerIteration const
            while (counter < max)
            {
                counter += increment;

                x2 = x / 2;
                y = (coef - 4 * x2);
                y /= (coef + x2);
                y -= coef;
                //y = Math.Abs(y);
                x += funcInc;
            }

            prevInt64Y = y;

            sw.Stop();

            var time = ((Double)sw.ElapsedTicks) / Stopwatch.Frequency;

            LastResultGigaOPS = TimeToGigaOPS(time, iterations, 1, inops: false);

            return time;
        }

        private Stopwatch threadsStopwatch = new Stopwatch();
        ManualResetEventSlim startThreads =  new ManualResetEventSlim();
        CountdownEvent threadsDoneCountdown = new CountdownEvent(1);
        CountdownEvent threadsReadyCountdown = new CountdownEvent(1);

        private void SingleThreadBody(int iterations, bool inops = false, bool precision64Bit = false)
        {
            var sw = new Stopwatch(); sw.Start();

            threadsReadyCountdown.Signal();
            startThreads.Wait();

            Debug.WriteLine("Started (ms): " + sw.ElapsedMilliseconds);

            if (!inops)
            {
                if (precision64Bit) RunFlops64Bit(iterations); else RunFlops32Bit(iterations);
            }
            else
            {
                if (precision64Bit) RunInops64Bit(iterations); else RunInops32Bit(iterations);
            }

            Debug.WriteLine("Done (ms): " + sw.ElapsedMilliseconds);

            threadsDoneCountdown.Signal();
        }

        /// <summary>
        /// Runs flops and inops calcuations in dedicated threads
        /// </summary>
        /// <remarks>
        /// Using tasks may lead to pauses and stalling (tens of seconds), seems like snatdard schedulaer doesn't kick off all tasks right away and they keep waiting for a long time. No such problem with threads
        /// </remarks>
        ///<returns>Seconds it took to complete the calculations</returns>
        public Double RunXopsMultiThreaded(int iterations, int threads, bool inops = false, bool precision64Bit = false, bool useTasks = false)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            var thrds = new Thread[threads]; 
             var tasks = new Task[threads];

            Debug.WriteLine("Multi-" + (useTasks ? "Tasks" : "Threads"));

            threadsDoneCountdown.Reset(threads);
            threadsReadyCountdown.Reset(threads);
            startThreads.Reset();

            for (int i = 0; i < threads; i++)
            {
                if (!useTasks)
                {
                    thrds[i] = new Thread(() => { SingleThreadBody(iterations, inops, precision64Bit); });
                    thrds[i].IsBackground = true;
                    thrds[i].Start();
                }
                else
                {
                    tasks[i] = new Task(() => { SingleThreadBody(iterations, inops, precision64Bit); });
                    tasks[i].Start();
                }
            }

            threadsReadyCountdown.Wait();
            // Starting stopwatch after Set() leads to spkes in 
            threadsStopwatch.Restart();
            startThreads.Set();

            threadsDoneCountdown.Wait();
            threadsStopwatch.Stop();

            var time = ((Double)threadsStopwatch.ElapsedTicks) / Stopwatch.Frequency;

            LastResultGigaOPS = TimeToGigaOPS(time, iterations, threads, inops);

            return time;
        }

        public double LastResultGigaOPS
        {
            get; private set;
        }

        public static double TimeToGigaOPS(double time, int iterations, int threads, bool inops)
        {
            return (double)(inops ? inopsPerIteration : flopsPerIteration) * iterations * threads / time / 1000000000;
        }

        ///// <summary>
        ///// Runs flops calcuation
        ///// </summary>
        ///// <param name="iterations">Number of times calculation is repated</param>
        ///// <param name="threads">If 0 - use the number of CPU cores</param>
        ///// <returns></returns>
        //public Double RunFlopsDoublePrecisionMultiThreaded(int iterations, int threads)
        //{
        //    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

        //    if (threads <= 0) throw new ArgumentException("Number of threafs must be positive", "threads");

        //    var tasks = new Task[threads];

        //    for (int i = 0; i < threads; i++)
        //    {

        //        tasks[i] = new Task(() => RunFlopsDoublePrecision(iterations));
        //    }

        //    var sw = new Stopwatch();
        //    sw.Restart();

        //    for (int i = 0; i < threads; i++)
        //    {
        //        tasks[i].Start();
        //    }

        //    Task.WaitAll(tasks);

        //    sw.Stop();

        //    return ((Double)sw.ElapsedTicks) / Stopwatch.Frequency;
        //}

        //public Double RunFlopsDoublePrecisionMultiThreaded(int iterations, int threads)
        //{
        //    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

        //    if (threads <= 0) throw new ArgumentException("Number of threafs must be positive", "threads");

        //    var tasks = new Task<Double>[threads];
        //    var thrds = new Thread[threads];

        //    for (int i = 0; i < threads; i++)
        //    {

        //        thrds[i] = new Thread(() => { RunFlopsDoublePrecision(iterations); });
        //    }

        //    var sw = new Stopwatch();
        //    sw.Start();

        //    for (int i = 0; i < threads; i++)
        //    {
        //        thrds[i].Start();
        //    }

        //    for (int i = 0; i < threads; i++)
        //    {
        //        thrds[i].Join();
        //    }

        //    sw.Stop();

        //    return ((Double)sw.ElapsedTicks) / Stopwatch.Frequency;
        //}

        public static int CpuCores
        {
            get { return Environment.ProcessorCount; }
        }
    }
}
