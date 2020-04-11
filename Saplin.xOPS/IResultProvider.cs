using System;
namespace Saplin.xOPS
{
    public interface IResultProvider
    {
        void Start();
        void Stop();
        double GetResult();
        void EndWarmUp();
    }
}
