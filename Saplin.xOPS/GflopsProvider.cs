namespace Saplin.xOPS
{
    public class GflopsProvider: GopsMtProvider
    {
        public GflopsProvider(int threads) : base(threads)
        {
            opsPerIteration = Compute.flopsPerIteration;
        }

        public override void Start()
        {
            compute.RunXopsMultiThreaded(-1, threads, false, false, false);
            counter = 0;
        }
    }
}
