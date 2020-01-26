using BenchmarkDotNet.Running;
using System;

namespace BenchmarkConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // アンマネージドメモリの確保
            //BenchmarkRunner.Run<AllocateMemory>();

            // マネージドからアンマネージドへのメモリコピー
            BenchmarkRunner.Run<MemCopyMaToUnma>();

            //using var mem = new MemCopyMaToUnma();
            //mem.TestMethods();


        }
    }
}
