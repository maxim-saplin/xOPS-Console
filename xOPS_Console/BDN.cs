using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Saplin.xOPS;

namespace xOPS_Console
{
    //[DisassemblyDiagnoser(printSource: true, printInstructionAddresses: true, exportHtml: true, maxDepth: 4)]
    //[DryJob(BenchmarkDotNet.Jobs.RuntimeMoniker.CoreRt31)]
    //public class BDN
    //{
    //    static Compute c = new Compute();

    //    //    private class Config : ManualConfig
    //    //    {
    //    //        public Config()
    //    //        {
    //    //            Add(DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
    //    //        }
    //    //    }

    //    static void Main()
    //    {
    //        //var config = DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator);
    //        //BenchmarkRunner.Run<BDN>(config);
    //        BenchmarkRunner.Run<BDN>();
    //    }

    //    //[Benchmark]
    //    //public void RunFloat32()
    //    //{
    //    //    var res = c.RunXops(1000000, false, false);
    //    //}

    //    [Benchmark]
    //    public void RunIntt32()
    //    {
    //        var res = c.RunXops(10000000, false, false);
    //    }
    //}
}
