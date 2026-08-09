using System.Runtime.InteropServices;

namespace Audio.Interop;

/// <summary>
/// Minimal PROPVARIANT covering only what listing needs: reading VT_LPWSTR.
/// Native header is vt(2) + 3 reserved WORDs(6) = 8 bytes before the union.
/// The real native union's largest member (DECIMAL) makes the whole struct
/// 16 bytes on x86 / 24 bytes on x64 — this struct must be at least that big,
/// otherwise GetValue's native write overruns our marshaled buffer and
/// corrupts adjacent heap memory. The trailing reserved field forces the
/// managed size to 24 bytes, which safely over-allocates on x86 too.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct PROPVARIANT : IDisposable
{
    [FieldOffset(0)]
    public ushort vt;

    [FieldOffset(8)]
    public IntPtr pointerValue;

    [FieldOffset(16)]
    private readonly long _reserved;

    public const ushort VT_LPWSTR = 31;

    public readonly string? GetStringValue()
    {
        if (vt != VT_LPWSTR || pointerValue == IntPtr.Zero)
            return null;

        return Marshal.PtrToStringUni(pointerValue);
    }

    public void Dispose() => NativeMethods.PropVariantClear(ref this);
}
