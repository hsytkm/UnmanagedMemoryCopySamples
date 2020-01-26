using BenchmarkDotNet.Running;
using System;

namespace BenchmarkConsole
{
    class Program
    {
        static void Main(string[] args)
        {
#if false
            /*
             * アンマネージドメモリの確保
             */
            BenchmarkRunner.Run<AllocateMemory>();
#endif

#if false
            /* 
             * マネージドからアンマネージドへのメモリコピー
             */
#if DEBUG
            using var mem = new MemCopyMaToUnma();
            mem.TestMethods();
#else
            BenchmarkRunner.Run<MemCopyMaToUnma>();
#endif
#endif

#if true
            /* 
             * アンマネージドからアンマネージドへのメモリコピー
             */
#if DEBUG
            using var mem = new MemCopyUnmaToUnma();
            mem.TestMethods();
#else
            BenchmarkRunner.Run<MemCopyUnmaToUnma>();
#endif
#endif

        }
    }
}
