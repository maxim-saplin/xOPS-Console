﻿namespace Saplin.xOPS
{
    public class GinopsProvider: GopsMtProvider
    {
        public GinopsProvider(int threads) : base(threads)
        {
            opsPerIteration = Compute.inopsPerIteration;
        }

        public override void Start()
        {
            compute.RunXopsMultiThreaded(-1, threads, false, false, false);
            counter = 0;
        }
    }
}
