namespace Audio.Core;

public sealed record AudioDevice(string Id, string FriendlyName, DeviceKind Kind, bool IsActive);
