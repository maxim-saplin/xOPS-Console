using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Saplin.xOPS
{
    public class TimeSeries
    {
        public TimeSeries()
        {
            SmoothingPoints = 3;
            MinValue = double.MaxValue;
            MaxValue = double.MinValue;
        }

        private int smoothingPoints;

        /// <summary>
        /// Use N points to smooth such values as Current, Start
        /// </summary>
        public int SmoothingPoints {
            get => smoothingPoints;
            set { smoothingPoints = value;  startValue = -1; }
        }

        private int warmpUpSamples;

        private double startValue = -1;

        public double StartValue
        {
            get
            {
                if (startValue > 0) return startValue;

                var count = results.Count - warmpUpSamples;

                if (count <= 0) return -1;

                double res = 0;
                int min = Math.Min(SmoothingPoints, count);

                for (int i = warmpUpSamples; i < min+warmpUpSamples; i++)
                {
                    res += results[i];
                }

                res /= min;

                if (min == SmoothingPoints) startValue = res;

                return res;
            }
        }

        public double CurrentValue
        {
            get
            {
                if (results.Count == 0) return -1;

                double res = 0;
                int min = Math.Min(SmoothingPoints, results.Count);

                for (int i = results.Count - 1; i >= results.Count - min; i--)
                {
                    res += results[i];
                }

                res /= min;

                return res;
            }
        }

        public double MinValue { get; protected set; }

        public double MaxValue { get; protected set; }

        private List<double> results = new List<double>();

        public ReadOnlyCollection<double> Results => results.AsReadOnly();

        public void Add(double value)
        {
            if (value < MinValue) MinValue = value;
            if (value > MaxValue) MaxValue = value;

            results.Add(value);
        }
    }
}
