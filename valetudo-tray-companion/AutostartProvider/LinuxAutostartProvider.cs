using System.Runtime.Versioning;
using System.Text;

namespace valetudo_tray_companion.AutostartProvider;

[SupportedOSPlatform("linux")]
public sealed class LinuxAutostartProvider : IAutostartProvider
{
    private static readonly string AutostartPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.config/autostart";

    private static readonly string AutostartDesktopFilePath = $"{AutostartPath}/{Constants.ApplicationName}.desktop";

    public bool IsSupported => true;
    public bool IsReady => true;
    public bool IsAutostartEnabled => File.Exists(AutostartDesktopFilePath);

    public void EnableAutostart()
    {
        File.WriteAllText(AutostartDesktopFilePath, GetDesktopFileContents());
    }

    public void DisableAutostart()
    {
        File.Delete(AutostartDesktopFilePath);
    }

    private string GetDesktopFileContents()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("[Desktop Entry]");
        sb.AppendLine("Type=Application");
        sb.AppendLine("Name=" + Constants.ApplicationName);
        sb.AppendLine("Exec=" + Environment.ProcessPath);
        
        return sb.ToString();
    }
}
