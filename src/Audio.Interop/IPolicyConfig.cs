using System.Runtime.InteropServices;

namespace Audio.Interop;

// Undocumented COM interface, no public Microsoft header. Vtable order below
// matches the widely-verified Windows 7 through Windows 10 layout (used by
// numerous shipping tools). Only SetDefaultEndpoint is actually invoked;
// every preceding method must stay in its correct vtable slot for that call
// to land correctly, but their exact parameter marshaling doesn't matter
// since they're never dispatched — do not reorder or remove any of them.
[ComImport]
[Guid(Guids.IID_IPolicyConfig)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IPolicyConfig
{
    [PreserveSig]
    int GetMixFormat(string pszDeviceName, out IntPtr ppFormat);

    [PreserveSig]
    int GetDeviceFormat(string pszDeviceName, int bDefault, out IntPtr ppFormat);

    [PreserveSig]
    int ResetDeviceFormat(string pszDeviceName);

    [PreserveSig]
    int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr pMixFormat);

    [PreserveSig]
    int GetProcessingPeriod(string pszDeviceName, int bDefault, out long hnsDefaultDevicePeriod, out long hnsMinimumDevicePeriod);

    [PreserveSig]
    int SetProcessingPeriod(string pszDeviceName, ref long hnsProcessingPeriod);

    [PreserveSig]
    int GetShareMode(string pszDeviceName, out IntPtr pMode);

    [PreserveSig]
    int SetShareMode(string pszDeviceName, IntPtr mode);

    [PreserveSig]
    int GetPropertyValue(string pszDeviceName, in PROPERTYKEY key, out PROPVARIANT pv);

    [PreserveSig]
    int SetPropertyValue(string pszDeviceName, in PROPERTYKEY key, in PROPVARIANT pv);

    [PreserveSig]
    int SetDefaultEndpoint(string pszDeviceName, ERole role);

    [PreserveSig]
    int SetEndpointVisibility(string pszDeviceName, int bVisible);
}
