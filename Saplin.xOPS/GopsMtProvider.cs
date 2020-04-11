namespace Saplin.xOPS
{
    public abstract class GopsMtProvider: IResultProvider
    {
        protected Compute compute = new Compute();
        protected int threads;
        protected int counter;
        protected int opsPerIteration = 1;

        public GopsMtProvider(int threads)
        {
            this.threads = threads;
        }

        public double GetResult()
        {
            var currentCounter = compute.ThreadLoopCounter;
            var gops = (double)(currentCounter - counter) * Compute.microIterationSize * opsPerIteration / 1000000000;

            counter = currentCounter;

            return gops;
        }

        public abstract void Start();

        public void Stop()
        {
            compute.BreakExecution();
        }

        public void EndWarmUp()
        {
            compute.ResetThreadLoopCounter();
        }
    }
}
