using System.Runtime.Versioning;
using Microsoft.Win32;

namespace valetudo_tray_companion.AutostartProvider;

[SupportedOSPlatform("windows")]
public sealed class WindowsAutostartProvider : IAutostartProvider
{
    private const string ApplicationName = "ValetudoTrayCompanion";
    private readonly RegistryKey? _autostartRegistryKey;
    private readonly string? _binaryLocation;

    public WindowsAutostartProvider()
    {
        _autostartRegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        _binaryLocation = Environment.ProcessPath;
    }
    
    public bool IsSupported => true;
    public bool IsReady => _autostartRegistryKey != null && _binaryLocation != null;
    
    public bool IsAutostartEnabled
    {
        get
        {
            if (IsReady)
                return _autostartRegistryKey!.GetValue(ApplicationName) != null;
            return false;
        }
    }
    
    public void EnableAutostart()
    {
        if (IsReady)
        {
            _autostartRegistryKey!.SetValue(ApplicationName, _binaryLocation!);
        }
    }
    
    public void DisableAutostart()
    {
        if (IsReady)
        {
            _autostartRegistryKey!.DeleteValue(ApplicationName, false);
        }
    }
}