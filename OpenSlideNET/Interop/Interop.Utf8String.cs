using System;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenSlideNET.Interop;

public static partial class OpenSlideInterop
{
    private static unsafe string? StringFromNativeUtf8(IntPtr nativeUtf8)
    {
        if (nativeUtf8 == IntPtr.Zero)
            return null;
        var len = 0;
        while (*(byte*)(nativeUtf8 + len) != 0)
            ++len;
        return Encoding.UTF8.GetString((byte*)nativeUtf8, len);
    }

    private ref struct Utf8String : IDisposable
    {
        private GCHandle handle;

        public Utf8String(string? value)
        {
            if (value == null)
            {
                handle = default;
                return;
            }

            handle = GCHandle.Alloc(Encoding.UTF8.GetBytes(value), GCHandleType.Pinned);
        }
        
        public static implicit operator IntPtr(Utf8String utf8String) => 
            utf8String.handle.IsAllocated ? utf8String.handle.AddrOfPinnedObject() : throw new ObjectDisposedException(nameof(Utf8String));

        public void Dispose()
        {
            if (!handle.IsAllocated) return;
            handle.Free();
            handle = default;
        }
    }
}