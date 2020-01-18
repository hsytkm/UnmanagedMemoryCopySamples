using BenchmarkDotNet.Attributes;
using System;
using System.Runtime.InteropServices;

namespace BenchmarkConsole
{
#if false
|         Method |     Mean |    Error |  StdDev |
|--------------- |---------:|---------:|--------:|
|   AllocHGrobal | 182.2 us | 56.12 us | 3.08 us |
| AllocCoTaskMem | 179.4 us | 21.47 us | 1.18 us |
#endif

    //[DryJob]        // 動作確認用の実行
    [ShortRunJob]   // 簡易測定
    public class AllocateMemory
    {
        private const int AllocSize = 10 * 1024 * 1024;

        public AllocateMemory() { }

        [Benchmark]
        public void AllocHGlobal()
        {
            IntPtr intPtr = Marshal.AllocHGlobal(AllocSize);
            Marshal.FreeHGlobal(intPtr);
        }

        [Benchmark]
        public void AllocCoTaskMem()
        {
            IntPtr intPtr = Marshal.AllocCoTaskMem(AllocSize);
            Marshal.FreeCoTaskMem(intPtr);
        }

    }
}
