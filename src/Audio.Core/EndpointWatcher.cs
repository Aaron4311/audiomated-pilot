using System.Runtime.InteropServices;
using Audio.Interop;

namespace Audio.Core;

// Wraps IMMNotificationClient registration. Kept internal to Core: it's the
// only place raw Interop types (EDataFlow/ERole) are used for watch mode —
// AudioManager translates to/from Core-level types before this leaks out.
internal sealed class EndpointWatcher : IMMNotificationClient, IDisposable
{
    private readonly MMDeviceEnumeratorComWrapper _enumeratorWrapper = new();
    private bool _started;

    public event Action<EDataFlow, ERole, string?>? DefaultDeviceChanged;

    public void Start()
    {
        int hr = _enumeratorWrapper.Enumerator.RegisterEndpointNotificationCallback(this);
        if (hr < 0)
            throw new COMException("RegisterEndpointNotificationCallback failed.", hr);
        _started = true;
    }

    public int OnDeviceStateChanged(string deviceId, uint newState) => 0;

    public int OnDeviceAdded(string deviceId) => 0;

    public int OnDeviceRemoved(string deviceId) => 0;

    public int OnDefaultDeviceChanged(EDataFlow flow, ERole role, string? defaultDeviceId)
    {
        DefaultDeviceChanged?.Invoke(flow, role, defaultDeviceId);
        return 0;
    }

    public int OnPropertyValueChanged(string deviceId, PROPERTYKEY key) => 0;

    public void Dispose()
    {
        if (_started)
        {
            _enumeratorWrapper.Enumerator.UnregisterEndpointNotificationCallback(this);
            _started = false;
        }

        _enumeratorWrapper.Dispose();
    }
}
