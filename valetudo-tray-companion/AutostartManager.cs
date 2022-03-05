using Microsoft.Win32;

namespace valetudo_tray_companion;

public class AutostartManager
{
    private const string ApplicationName = "ValetudoTrayCompanion";
    private readonly RegistryKey? _autostartRegistryKey;
    private readonly string? _binaryLocation;
    
    public AutostartManager()
    {
        _autostartRegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        _binaryLocation = Environment.ProcessPath;
    }

    public bool IsReady()
    {
        return _autostartRegistryKey != null && _binaryLocation != null;
    }

    public bool IsAutostartEnabled()
    {
        if (IsReady())
        {
            return _autostartRegistryKey!.GetValue(ApplicationName) != null;
        }
        else
        {
            return false;
        }
    }

    public void EnableAutostart()
    {
        if (IsReady())
        {
            _autostartRegistryKey!.SetValue(ApplicationName, _binaryLocation!);
        }
    }

    public void DisableAutostart()
    {
        if (IsReady())
        {
            _autostartRegistryKey!.DeleteValue(ApplicationName, false);
        }
    }
}