using System.Runtime.InteropServices;

namespace Audio.Interop;

[ComImport]
[Guid(Guids.IID_IMMDeviceCollection)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDeviceCollection
{
    [PreserveSig]
    int GetCount(out uint pcDevices);

    [PreserveSig]
    int Item(uint nDevice, out IMMDevice ppDevice);
}
