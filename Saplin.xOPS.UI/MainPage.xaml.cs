using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Saplin.xOPS.UI
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {

        public void WriteLine(string s)
        {
            Device.BeginInvokeOnMainThread(() => text.Text += s + "\n");
            view.ScrollToAsync(text, ScrollToPosition.End, false);
        }

        public void Write(string s)
        {
            Device.BeginInvokeOnMainThread(() => text.Text += s);
        }

        private void Run(Compute c, int n, int threads, bool inops, bool useTasks)
        {
            const int repeats = 4;

            Double[] times = new Double[repeats];
            Double[] gxops = new Double[repeats];

            WriteLine(string.Format("\n{0} tests in {1} {2}, N: {3}",
                inops ? "INT32" : "FLT32",
                threads,
                useTasks ? "tasks" : "threads",
                n));

            Write(string.Format(inops ? "G_INOPS: " : "G_FLOPS: "));
            for (int i = 0; i < repeats; i++)
            {
                if (inops && threads == 1)
                {
                    times[i] = c.RunInops32Bit(n);
                    gxops[i] = (double)Compute.inopsPerIteration * n / times[i] / 1000000000;
                }
                else if (inops && threads > 1)
                {
                    times[i] = c.RunXopsMultiThreaded(n, threads, inops: true, useTasks: useTasks);
                    gxops[i] = (double)Compute.inopsPerIteration * n * threads / times[i] / 1000000000;
                }
                else if (!inops && threads == 1)
                {
                    times[i] = c.RunFlops64Bit(n);
                    gxops[i] = (double)Compute.flopsPerIteration * n / times[i] / 1000000000;
                }
                else if (!inops && threads > 1)
                {
                    times[i] = c.RunXopsMultiThreaded(n, threads, inops: false, useTasks: useTasks);
                    gxops[i] = (double)Compute.flopsPerIteration * n * threads / times[i] / 1000000000;
                }

                Write(string.Format("{0:0.00}; ", gxops[i]));
            }

            WriteLine(string.Format("\nAverage {0}: {1}", inops ? "G_INOPS" : "G_FLOPS", gxops.Average()));
        }

        public MainPage()
        {
            InitializeComponent();

            Task.Run(() =>
           {

               var c = new Compute();
               var n = 10 * 1000 * 1000;

               Run(c, n, 1, inops: false, useTasks: false);
               Run(c, n, 1, inops: true, useTasks: false);

               int tpT, tpP;

               ThreadPool.GetMaxThreads(out tpT, out tpP);
               WriteLine("\nCores: " + Environment.ProcessorCount + "; thread pool threads: " + tpT + "; ports: " + tpP);

               var threads = 4;

               Run(c, n, threads, inops: false, useTasks: false);
               Run(c, n, threads, inops: false, useTasks: true);
               Run(c, n, threads, inops: true, useTasks: true);

               threads = 8;

               Run(c, n, threads, inops: false, useTasks: false);
               Run(c, n, threads, inops: false, useTasks: true);
               Run(c, n, threads, inops: true, useTasks: true);

               threads = 32;

               Run(c, n, threads, inops: false, useTasks: false);
               Run(c, n, threads, inops: false, useTasks: true);
               Run(c, n, threads, inops: true, useTasks: true);

               threads = 128;

               Run(c, n, threads, inops: false, useTasks: false);
               Run(c, n, threads, inops: false, useTasks: true);
               Run(c, n, threads, inops: true, useTasks: true);

               threads = 256;

               Run(c, n, threads, inops: false, useTasks: false);
               Run(c, n, threads, inops: false, useTasks: true);
               Run(c, n, threads, inops: true, useTasks: true);

               WriteLine("DONE");
           });
        }
    }
}
