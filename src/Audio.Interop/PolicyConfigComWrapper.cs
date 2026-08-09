using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Audio.Interop;

[SupportedOSPlatform("windows")]
public sealed class PolicyConfigComWrapper : IDisposable
{
    private IPolicyConfig? _policyConfig;

    public PolicyConfigComWrapper()
    {
        var comType = Type.GetTypeFromCLSID(new Guid(Guids.CLSID_PolicyConfigClient))
            ?? throw new InvalidOperationException("CLSID_PolicyConfigClient not found on this system.");

        object comObject = Activator.CreateInstance(comType)
            ?? throw new InvalidOperationException("Failed to create PolicyConfig COM instance.");

        _policyConfig = (IPolicyConfig)comObject;
    }

    public IPolicyConfig PolicyConfig =>
        _policyConfig ?? throw new ObjectDisposedException(nameof(PolicyConfigComWrapper));

    public void Dispose()
    {
        if (_policyConfig is not null)
        {
            if (Marshal.IsComObject(_policyConfig))
                Marshal.ReleaseComObject(_policyConfig);
            _policyConfig = null;
        }
    }
}
