using System.Runtime.InteropServices;
using valetudo_tray_companion.AutostartProvider;

namespace valetudo_tray_companion;

public sealed class AutostartManager
{
    private readonly IAutostartProvider _autostartProvider;

    public AutostartManager()
    {
        if (OperatingSystem.IsWindows())
            _autostartProvider = new WindowsAutostartProvider();
        else if (OperatingSystem.IsLinux())
            _autostartProvider = new LinuxAutostartProvider();
        else if (OperatingSystem.IsMacOS())
            _autostartProvider = new MacosAutostartProvider();
        else
            throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
    }
    
    public bool IsSupported => _autostartProvider.IsSupported;
    public bool IsReady => _autostartProvider.IsReady;
    public bool IsAutostartEnabled => _autostartProvider.IsAutostartEnabled;
    public void EnableAutostart() => _autostartProvider.EnableAutostart();
    public void DisableAutostart() => _autostartProvider.DisableAutostart();
}