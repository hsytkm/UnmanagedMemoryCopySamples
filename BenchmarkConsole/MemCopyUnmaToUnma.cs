using BenchmarkDotNet.Attributes;
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace BenchmarkConsole
{
    /*
     * アンマネージドメモリ(IntPtr) から アンマネージドメモリ(IntPtr) へのコピー
     * 
     * 高速なのは PInvokeRtlMoveMemory()！ unsafeも必要ないので良さげ。
     */
#if false
|                 Method |     Mean |     Error |    StdDev |
|----------------------- |---------:|----------:|----------:|
|        Unsafe1ByteCopy | 7.455 ms | 2.7616 ms | 0.1514 ms |
|        Unsafe4ByteCopy | 2.150 ms | 0.3166 ms | 0.0174 ms |
| UnsafeBufferMemoryCopy | 1.571 ms | 0.4144 ms | 0.0227 ms |
|   UnsafeClassCopyBlock | 1.527 ms | 0.1577 ms | 0.0086 ms |
|   PInvokeRtlMoveMemory | 1.417 ms | 0.1538 ms | 0.0084 ms |
|          PInvokeMemcpy | 1.430 ms | 0.2715 ms | 0.0149 ms |
#endif

    //[DryJob]        // 動作確認用の実行
    [ShortRunJob]   // 簡易測定
    public class MemCopyUnmaToUnma : IDisposable
    {
        private const int AllocSize = 10 * 1024 * 1024;

        private readonly IntPtr _srcPtr;
        private readonly IntPtr _dstPtr;
        private readonly ulong _answer;

        #region APIs
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        private static extern void CopyMemory(IntPtr dst, IntPtr src, uint size);

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        private static extern void RtlMoveMemory(IntPtr dst, IntPtr src, [MarshalAs(UnmanagedType.U4)] int length);

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", SetLastError = false)]
        private static extern IntPtr memcpy(IntPtr dst, IntPtr src, UIntPtr count);
        #endregion

        public unsafe MemCopyUnmaToUnma()
        {
            _srcPtr = Marshal.AllocCoTaskMem(AllocSize);
            _dstPtr = Marshal.AllocCoTaskMem(AllocSize);

            byte* psrc = (byte*)_srcPtr;
            for (int i = 0; i < AllocSize; ++i)
            {
                *(psrc + i) = (byte)(i & 0xff);
            }

            ulong sum = 0;
            for (int i = 0; i < AllocSize; ++i) sum += psrc[i];
            _answer = sum;
        }

        public void TestMethods()
        {
            //CheckAnswer(MarshalCopy);

            CheckAnswer(Unsafe1ByteCopy);
            CheckAnswer(Unsafe4ByteCopy);
            CheckAnswer(UnsafeBufferMemoryCopy);
            CheckAnswer(UnsafeClassCopyBlock);

            //CheckAnswer(PInvokeKernelCopy);
            CheckAnswer(PInvokeRtlMoveMemory);
            CheckAnswer(PInvokeMemcpy);

            unsafe void CheckAnswer(Action action)
            {
                action.Invoke();
                Debug.Assert(_answer == GetSum(_dstPtr, AllocSize));

                Unsafe.InitBlock(_dstPtr.ToPointer(), 0, AllocSize);
                Debug.Assert(0 == GetSum(_dstPtr, AllocSize));

                static unsafe ulong GetSum(IntPtr intPtr, int size)
                {
                    byte* head = (byte*)intPtr.ToPointer();
                    byte* tail = head + size;
                    ulong sum = 0;
                    while (head < tail)
                        sum += *(head++);

                    return sum;
                }
            }
        }

        //[Benchmark]
        //public unsafe void MarshalCopy()
        //{
        //    Marshal.Copy(_srcArray, 0, _dstPtr, AllocSize);
        //}

        [Benchmark]
        public unsafe void Unsafe1ByteCopy()
        {
            byte* src = (byte*)_srcPtr;
            byte* dst = (byte*)_dstPtr;
            for (int i = 0; i < AllocSize; ++i)
            {
                dst[i] = src[i];
            }
        }

        [Benchmark]
        public unsafe void Unsafe4ByteCopy()
        {
            byte* srcHead = (byte*)_srcPtr;
            byte* src = srcHead;
            byte* dst = (byte*)_dstPtr;
            int rest = AllocSize;

            while (rest >= sizeof(ulong))
            {
                *((ulong*)dst) = *((ulong*)src);
                src += sizeof(ulong);
                dst += sizeof(ulong);
                rest -= sizeof(ulong);
            }

            while (rest >= 1)
            {
                *(dst++) = *(src++);
                rest--;
            }
        }

        [Benchmark]
        public unsafe void UnsafeBufferMemoryCopy()
        {
            Buffer.MemoryCopy(_srcPtr.ToPointer(), _dstPtr.ToPointer(), AllocSize, AllocSize);
        }

        [Benchmark]
        public unsafe void UnsafeClassCopyBlock()
        {
            Unsafe.CopyBlock(_dstPtr.ToPointer(), _srcPtr.ToPointer(), AllocSize);
        }

#if false
        // EntryPoint not found と言われて実行時Errorになった…不明
        [Benchmark]
        public void PInvokeKernelCopy()
        {
            CopyMemory(_dstPtr, _srcPtr, AllocSize);
        }
#endif

        [Benchmark]
        public void PInvokeRtlMoveMemory()
        {
            RtlMoveMemory(_dstPtr, _srcPtr, AllocSize);
        }

        [Benchmark]
        public void PInvokeMemcpy()
        {
            memcpy(_dstPtr, _srcPtr, (UIntPtr)AllocSize);
        }

        public void Dispose()
        {
            Marshal.FreeCoTaskMem(_srcPtr);
            Marshal.FreeCoTaskMem(_dstPtr);
        }
    }
}
