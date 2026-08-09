using System.Runtime.InteropServices;

namespace Audio.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct PROPERTYKEY
{
    public Guid fmtid;
    public uint pid;

    public PROPERTYKEY(Guid fmtid, uint pid)
    {
        this.fmtid = fmtid;
        this.pid = pid;
    }
}

public static class PropertyKeys
{
    public static readonly PROPERTYKEY PKEY_Device_FriendlyName =
        new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);
}
