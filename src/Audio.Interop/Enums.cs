namespace Audio.Interop;

public enum EDataFlow
{
    eRender = 0,
    eCapture = 1,
    eAll = 2,
}

public enum ERole
{
    eConsole = 0,
    eMultimedia = 1,
    eCommunications = 2,
}

[Flags]
public enum DEVICE_STATE : uint
{
    ACTIVE = 0x00000001,
    DISABLED = 0x00000002,
    NOTPRESENT = 0x00000004,
    UNPLUGGED = 0x00000008,
    MASK_ALL = 0x0000000F,
}
