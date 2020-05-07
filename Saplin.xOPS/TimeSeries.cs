using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Saplin.xOPS
{
    public class TimeSeries
    {
        public TimeSeries(int smoothingPoints)
        {
            SmoothingPoints = smoothingPoints;
            Min = MinSmooth = double.MaxValue;
            Max = MaxSmooth = double.MinValue;
        }

        /// <summary>
        /// Use N points to smooth such values as Current, Start
        /// </summary>
        public int SmoothingPoints { get; private set; }

        public double StartSmooth
        {
            get
            {
                if (smoothResults.Count > 0) return smoothResults[0];

                return -1;
            }
        }

        public double CurrentSmooth
        {
            get
            {
                if (smoothResults.Count > 0) return smoothResults[smoothResults.Count-1];

                return -1;
            }
        }

        public double Min { get; protected set; }

        public double Max { get; protected set; }

        public double MinSmooth { get; protected set; }

        public double MaxSmooth { get; protected set; }

        private List<double> results = new List<double>();
        private List<double> smoothResults = new List<double>();

        public ReadOnlyCollection<double> Results => results.AsReadOnly();

        public ReadOnlyCollection<double> SmoothResults => smoothResults.AsReadOnly();

        int counter = 0;

        public void Add(double value)
        {
            if (value < Min) Min = value;
            if (value > Max) Max = value;

            results.Add(value);

            counter++;

            if (counter >= SmoothingPoints)
            {
                counter = 0;

                double smooth = 0;

                for (int i = results.Count - 1; i > results.Count - SmoothingPoints - 1; i--)
                    smooth += results[i];

                smooth /= SmoothingPoints;

                if (smooth < MinSmooth) MinSmooth = smooth;
                if (smooth > MaxSmooth) MaxSmooth = smooth;

                smoothResults.Add(smooth);
            }
        }
    }
}
