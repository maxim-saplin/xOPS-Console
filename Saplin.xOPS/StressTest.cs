namespace Saplin.xOPS
{
    public class StressTest: ContinuousRun
    {
        /// <summary>
        /// Set floating and integer tests
        /// </summary>
        /// <param name="threadsEach">Number of threads to created in floating and integer test (total threads created is a double)</param>
        public StressTest(int threadsEach, bool gflops = true, bool ginops = true)
        {
            if (gflops)
            {
                GflopsProvider = new GflopsProvider(threadsEach);
                AddProvider(GflopsProvider);
                GflopsResults = TimeSeries[TimeSeries.Count - 1];
            }

            if (ginops)
            {
                GinopsProvider = new GinopsProvider(threadsEach);
                AddProvider(GinopsProvider);
                GinopsResults = TimeSeries[TimeSeries.Count - 1];
            }
        }

        public IResultProvider GflopsProvider { get; private set; }
        public IResultProvider GinopsProvider { get; private set; }

        public TimeSeries GflopsResults { get; private set; }
        public TimeSeries GinopsResults { get; private set; }
    }
}