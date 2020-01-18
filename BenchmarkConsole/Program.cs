using BenchmarkDotNet.Running;
using System;

namespace BenchmarkConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<AllocateMemory>();

#if true
            BenchmarkRunner.Run<MemCopyMaToUnma>();
#else
            using var mem = new MemCopyMaToUnma();
            mem.TestMethods();
#endif


        }
    }
}
