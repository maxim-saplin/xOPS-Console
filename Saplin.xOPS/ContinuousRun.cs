using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

namespace Saplin.xOPS
{
    public class ContinuousRun
    {
        public int SamplingPeriodMs { get; private set; }

        /// <summary>
        /// Use N points to smooth such values as Current, Start
        /// </summary>
        public int SmoothingPoints { get; private set; }

        /// <summary>
        /// Skip few samples and let the run warm up
        /// </summary>
        public int WarmpUpSamples { get; private set; }

        public bool WarmpingUp { get; private set; }

        private Timer timer = new Timer();
        private Stopwatch sw = new Stopwatch();

        public TimeSpan Elapsed => sw.Elapsed;

        public ContinuousRun(int samplingPeriodMs, int smoothingPoints, int warmpUpSamples)
        {
            SamplingPeriodMs = samplingPeriodMs;
            timer.Elapsed += OnTimedEvent;
            SmoothingPoints = smoothingPoints;
            WarmpUpSamples = warmpUpSamples;
        }

        private int warmUpCounter;

        double periodSeconds = 1;

        public void Start()
        {
            sw.Restart();
            warmUpCounter = WarmpUpSamples;
            if (warmUpCounter > 0) WarmpingUp = true;

            foreach (var p in providers)
                p.Start();

            periodSeconds = (double)SamplingPeriodMs / 1000;
            
            timer.Interval = SamplingPeriodMs;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public void Stop()
        {
            timer.AutoReset = false;
            timer.Enabled = false;

            foreach (var p in providers)
                p.Stop();

            sw.Stop();
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (!WarmpingUp)
            {
                for (int i = 0; i < results.Count; i++)
                {
                    var r = providers[i].GetResult();
                    r /= periodSeconds;
                    results[i].Add(r);
                }
            }
            else
            {
                if (warmUpCounter >= 1)
                {
                    warmUpCounter--;
                }
                else
                {
                    foreach (var p in providers)
                    {
                        p.EndWarmUp();
                    }
                    WarmpingUp = false;
                }
            }

            ResultsUpdated?.Invoke(this);
        }

        protected List<IResultProvider> providers = new List<IResultProvider>();
        protected List<TimeSeries> results = new List<TimeSeries>();

        public IReadOnlyList<TimeSeries> TimeSeries => results.AsReadOnly();

        public void AddProvider(IResultProvider provider)
        {
            providers.Add(provider);
            results.Add(new TimeSeries(SmoothingPoints));
        }

        public delegate void EventHandler(ContinuousRun sender);

        public EventHandler ResultsUpdated;
    }
}
