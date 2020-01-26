using BenchmarkDotNet.Attributes;
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace BenchmarkConsole
{
    /*
     * マネージドメモリ(byte[]) から アンマネージドメモリ(IntPtr) へのコピー
     * 
     * 高速なのは UnsafePInvokeRtlMoveMemory() だけど、
     * unsafe 無しならベタに MarshalCopy() が良さげ。
     */
#if false
|                     Method |     Mean |     Error |    StdDev |
|--------------------------- |---------:|----------:|----------:|
|                MarshalCopy | 1.840 ms | 0.8311 ms | 0.0456 ms |
|            Unsafe1ByteCopy | 7.578 ms | 1.0404 ms | 0.0570 ms |
|            Unsafe4ByteCopy | 2.229 ms | 0.5605 ms | 0.0307 ms |
|     UnsafeBufferMemoryCopy | 1.814 ms | 1.0182 ms | 0.0558 ms |
|       UnsafeClassCopyBlock | 1.784 ms | 0.1231 ms | 0.0067 ms |
| UnsafePInvokeRtlMoveMemory | 1.508 ms | 0.1276 ms | 0.0070 ms |
|        UnsafePInvokeMemcpy | 1.510 ms | 0.1884 ms | 0.0103 ms |
#endif

    //[DryJob]        // 動作確認用の実行
    [ShortRunJob]   // 簡易測定
    public class MemCopyMaToUnma : IDisposable
    {
        private const int AllocSize = 10 * 1024 * 1024;

        private readonly byte[] _srcArray;
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

        public MemCopyMaToUnma()
        {
            _srcArray = new byte[AllocSize];
            for (int i = 0; i < AllocSize; ++i)
            {
                _srcArray[i] = (byte)(i & 0xff);
            }

            ulong sum = 0;
            for (int i = 0; i < AllocSize; ++i) sum += _srcArray[i];
            _answer = sum;

            _dstPtr = Marshal.AllocCoTaskMem(AllocSize);
        }

        public void TestMethods()
        {
            CheckAnswer(MarshalCopy);

            CheckAnswer(Unsafe1ByteCopy);
            CheckAnswer(Unsafe4ByteCopy);
            CheckAnswer(UnsafeBufferMemoryCopy);
            CheckAnswer(UnsafeClassCopyBlock);

            //CheckAnswer(UnsafePInvokeKernelCopy);
            CheckAnswer(UnsafePInvokeRtlMoveMemory);
            CheckAnswer(UnsafePInvokeMemcpy);

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

        [Benchmark]
        public unsafe void MarshalCopy()
        {
            Marshal.Copy(_srcArray, 0, _dstPtr, AllocSize);
        }

        [Benchmark]
        public unsafe void Unsafe1ByteCopy()
        {
            fixed (byte* src = _srcArray)
            {
                byte* dst = (byte*)_dstPtr;
                for (int i = 0; i < AllocSize; ++i)
                {
                    dst[i] = src[i];
                }
            }
        }

        [Benchmark]
        public unsafe void Unsafe4ByteCopy()
        {
            fixed (byte* srcHead = _srcArray)
            {
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
        }

        [Benchmark]
        public unsafe void UnsafeBufferMemoryCopy()
        {
            fixed (void* src = _srcArray)
            {
                Buffer.MemoryCopy(src, _dstPtr.ToPointer(), AllocSize, AllocSize);
            }
        }

        [Benchmark]
        public unsafe void UnsafeClassCopyBlock()
        {
            fixed (void* src = _srcArray)
            {
                Unsafe.CopyBlock(_dstPtr.ToPointer(), src, AllocSize);
            }
        }

#if false
        // EntryPoint not found と言われて実行時Errorになった…不明
        [Benchmark]
        public unsafe void UnsafePInvokeKernelCopy()
        {
            fixed (void* src = _srcArray)
            {
                CopyMemory(_dstPtr, (IntPtr)src, AllocSize);
            }
        }
#endif

        [Benchmark]
        public unsafe void UnsafePInvokeRtlMoveMemory()
        {
            fixed (void* src = _srcArray)
            {
                RtlMoveMemory(_dstPtr, (IntPtr)src, AllocSize);
            }
        }

        [Benchmark]
        public unsafe void UnsafePInvokeMemcpy()
        {
            fixed (void* src = _srcArray)
            {
                memcpy(_dstPtr, (IntPtr)src, (UIntPtr)AllocSize);
            }
        }

        public void Dispose()
        {
            Marshal.FreeCoTaskMem(_dstPtr);
        }
    }
}
