using Avalonia;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ValetudoTrayCompanion;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    { 
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            LogException(e.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogException(e.Exception);
            e.SetObserved();
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogException(ex);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    
    private static void LogException(Exception? ex)
    {
        if (ex == null)
            return;

        var builder = new StringBuilder();
        builder.Append($"Crash::: {ex.GetType().FullName}: {ex.Message}\n\n");
        builder.Append("----------------------------\n");
        builder.Append($"Version: {Assembly.GetExecutingAssembly().GetName().Version}\n");
        builder.Append($"OS: {Environment.OSVersion.ToString()}\n");
        builder.Append($"Framework: {AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName}\n");
        builder.Append($"Source: {ex.Source}\n");
        builder.Append("---------------------------\n\n");
        builder.Append(ex.StackTrace);
        while (ex.InnerException != null)
        {
            ex = ex.InnerException;
            builder.Append($"\n\nInnerException::: {ex.GetType().FullName}: {ex.Message}\n");
            builder.Append(ex.StackTrace);
        }

        var time = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var file =  $"crash_{time}.log";
        File.WriteAllText(file, builder.ToString());
    }
}
