using System.Runtime.InteropServices;

namespace Audio.Interop;

internal static class NativeMethods
{
    [DllImport("ole32.dll")]
    public static extern int PropVariantClear(ref PROPVARIANT pvar);
}
