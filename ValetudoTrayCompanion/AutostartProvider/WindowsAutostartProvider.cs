using System;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ValetudoTrayCompanion.AutostartProvider;

[SupportedOSPlatform("windows")]
public sealed class WindowsAutostartProvider : IAutostartProvider
{
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
                return _autostartRegistryKey!.GetValue(Constants.ApplicationName) != null;
            return false;
        }
    }
    
    public void EnableAutostart()
    {
        if (IsReady)
        {
            _autostartRegistryKey!.SetValue(Constants.ApplicationName, _binaryLocation!);
        }
    }
    
    public void DisableAutostart()
    {
        if (IsReady)
        {
            _autostartRegistryKey!.DeleteValue(Constants.ApplicationName, false);
        }
    }
}