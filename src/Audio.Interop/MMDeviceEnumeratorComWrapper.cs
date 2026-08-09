using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Audio.Interop;

[SupportedOSPlatform("windows")]
public sealed class MMDeviceEnumeratorComWrapper : IDisposable
{
    private IMMDeviceEnumerator? _enumerator;

    public MMDeviceEnumeratorComWrapper()
    {
        var comType = Type.GetTypeFromCLSID(new Guid(Guids.CLSID_MMDeviceEnumerator))
            ?? throw new InvalidOperationException("CLSID_MMDeviceEnumerator not found on this system.");

        object comObject = Activator.CreateInstance(comType)
            ?? throw new InvalidOperationException("Failed to create MMDeviceEnumerator COM instance.");

        _enumerator = (IMMDeviceEnumerator)comObject;
    }

    public IMMDeviceEnumerator Enumerator =>
        _enumerator ?? throw new ObjectDisposedException(nameof(MMDeviceEnumeratorComWrapper));

    public void Dispose()
    {
        if (_enumerator is not null)
        {
            if (Marshal.IsComObject(_enumerator))
                Marshal.ReleaseComObject(_enumerator);
            _enumerator = null;
        }
    }
}
