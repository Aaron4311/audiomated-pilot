using System.Runtime.InteropServices;

namespace Audio.Interop;

[ComImport]
[Guid(Guids.IID_IMMDevice)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDevice
{
    [PreserveSig]
    int Activate(in Guid iid, uint dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);

    [PreserveSig]
    int OpenPropertyStore(uint stgmAccess, out IPropertyStore ppProperties);

    // Native LPWSTR is allocated with CoTaskMemAlloc; the CLR's LPWStr
    // marshaler frees it via CoTaskMemFree automatically after copying into
    // the managed string, so no manual free is needed here.
    [PreserveSig]
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

    [PreserveSig]
    int GetState(out uint pdwState);
}
