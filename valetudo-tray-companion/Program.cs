using Avalonia;
using System.Diagnostics;
using Avalonia.Logging;

namespace valetudo_tray_companion;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Add console log output for simple debugging purposes.
        // The `OutputType` property in the .csproj file has to be changed from `WinExe` to `Exe` for the console to be visible.
        var listener = new ConsoleTraceListener();
        Trace.Listeners.Add(listener);
        
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Trace.TraceError("{0:HH:mm:ss.fff} Exception {1}", DateTime.Now, e);
        }
        finally
        {
            Trace.Flush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
