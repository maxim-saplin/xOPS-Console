using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;
using Saplin.xOPS;

namespace xOPS_Console
{
    public class BenchmarkDotNet
    {
        static Compute c = new Compute();

        public BenchmarkDotNet()
        {
        }
            //    private class Config : ManualConfig
            //    {
            //        public Config()
            //        {
            //            Add(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
            //        }
            //    }

            //static void Main()
            //{
            //    //var config = DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator);
            //    //BenchmarkRunner.Run<Program>(config);
            //    BenchmarkRunner.Run<Program>();
            //}

        [Benchmark]
        public void RunFloat32()
        {
            var res = c.RunXops(1000000, false, false);
        }

        [Benchmark]
        public void RunIntt32()
        {
            var res = c.RunXops(10000000, false, false);
        }
    }
}
