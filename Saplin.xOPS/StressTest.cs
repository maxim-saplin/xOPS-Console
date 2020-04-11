namespace Saplin.xOPS
{
    public class StressTest: ContinuousRun
    {
        /// <summary>
        /// Set floating and integer tests
        /// </summary>
        /// <param name="threadsEach">Number of threads to created in floating and integer test (total threads created is a double)</param>
        public StressTest(int threadsEach)
        {
            AddProvider(new GflopsProvider(threadsEach));
            AddProvider(new GinopsProvider(threadsEach));
        }
    }
}