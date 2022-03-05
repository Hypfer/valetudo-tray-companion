using Microsoft.Win32;

namespace valetudo_tray_companion;

public class AutostartManager
{
    private const string ApplicationName = "ValetudoTrayCompanion";
    private readonly RegistryKey? _autostartRegistryKey;
    
    public AutostartManager()
    {
        _autostartRegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
    }

    public bool IsReady()
    {
        return _autostartRegistryKey != null;
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
            _autostartRegistryKey!.SetValue(ApplicationName, System.Reflection.Assembly.GetExecutingAssembly().Location);
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