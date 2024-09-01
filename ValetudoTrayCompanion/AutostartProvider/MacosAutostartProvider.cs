using System;
using System.Runtime.Versioning;

namespace ValetudoTrayCompanion.AutostartProvider;

/// <summary>
/// TODO: Implement
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacosAutostartProvider : IAutostartProvider
{
    public bool IsSupported => false;
    public bool IsReady => false;
    public bool IsAutostartEnabled => false;
    public void EnableAutostart() => throw new NotImplementedException();
    public void DisableAutostart() => throw new NotImplementedException();
}