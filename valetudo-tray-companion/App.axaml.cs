using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Zeroconf;

namespace valetudo_tray_companion;

public partial class App : Application
{
    private TrayIcon _icon = null!;
    private readonly List<NativeMenuItemBase> _utilityToolStripItems = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly AutostartManager _autostartManager = new();
    private List<DiscoveredValetudoInstance> _discoveredInstances = new();
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        InitIcon();
        UpdateIconState();
        StartDiscoveryLoop();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        
        base.OnFrameworkInitializationCompleted();
    }
    
    private void InitIcon()
    {
        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
        using var valetudoLogoStream = assets!.Open(new Uri("resm:valetudo-tray-companion.res.logo.ico"));
        _icon = new TrayIcon();
        _icon.Icon = new WindowIcon(valetudoLogoStream);
        _icon.IsVisible = true;
        _utilityToolStripItems.Add(new NativeMenuItemSeparator());
        
        if (_autostartManager is { IsSupported: true, IsReady: true })
        {
            var autoStartItem = new NativeMenuItem();
            autoStartItem.IsChecked = _autostartManager.IsAutostartEnabled;
            SetAutostartMenuItemHeader(autoStartItem);

            autoStartItem.Click += (_, _) =>
            {
                if (_autostartManager.IsAutostartEnabled)
                {
                    _autostartManager.DisableAutostart();
                }
                else
                {
                    _autostartManager.EnableAutostart();
                }
                
                autoStartItem.IsChecked = _autostartManager.IsAutostartEnabled;
                SetAutostartMenuItemHeader(autoStartItem);
            };
        
            _utilityToolStripItems.Add(autoStartItem);
        }
        
        var closeItem = new NativeMenuItem("Exit");
        closeItem.Click += (_, _) =>
        {
            _cts.Cancel();
            if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime) 
                lifetime.Shutdown();
        };
        
        _utilityToolStripItems.Add(closeItem);
    }
    
    /// <summary>
    /// <see cref="NativeMenuItem.IsChecked"/> is currently not implemented, just use unicode ballot box for now.
    /// https://github.com/AvaloniaUI/Avalonia/issues/7880
    /// </summary>
    private static void SetAutostartMenuItemHeader(NativeMenuItem autoStartItem)
    {
        autoStartItem.Header = autoStartItem.IsChecked ? "☑ Run on startup" : "☐ Run on startup";
    }
    
    private void UpdateIconState()
    {
        _icon.ToolTipText = _discoveredInstances.Count switch
        {
            > 1 => $"{_discoveredInstances.Count} instances discovered",
            > 0 => $"{_discoveredInstances.Count} instance discovered",
            _ => "Discovering instances..."
        };
        
        var ctxMenu = new NativeMenu();
        _icon.Menu = ctxMenu;
        
        foreach (var discoveredValetudoInstance in _discoveredInstances)
        {
            var item = new NativeMenuItem(discoveredValetudoInstance.FriendlyName);
            ctxMenu.Items.Add(item);

            item.Click += (_, _) =>
            {
                OpenUrl("http://" + discoveredValetudoInstance.Address);
            };
        }
        
        foreach (var utilityToolStripItem in _utilityToolStripItems)
        {
            utilityToolStripItem.Parent = null;
            ctxMenu.Items.Add(utilityToolStripItem);
        }
    }
    
    private async void StartDiscoveryLoop()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            await DiscoverInstances();
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                var now = DateTime.Now;
                _discoveredInstances = _discoveredInstances.Where(x => (now - x.LastSeen).TotalMinutes <= 5).ToList();
            
                await DiscoverInstances();
            }
        } catch (OperationCanceledException)
        {
            //intentional
        }
    }
    
    private async Task DiscoverInstances()
    {
        IReadOnlyList<IZeroconfHost> results = await ZeroconfResolver.ResolveAsync("_valetudo._tcp.local.", cancellationToken: _cts.Token);
        
        foreach (var zeroconfHost in results)
        {
            if (zeroconfHost.Services.Count <= 0) 
                continue;
            
            var service = zeroconfHost.Services.FirstOrDefault().Value;
            var props = new Dictionary<string, string>();
                
            foreach (var readOnlyDictionary in service.Properties)
            {
                foreach (var (key, value) in readOnlyDictionary)
                {
                    props[key] = value;
                }
            }
            
            if (props.ContainsKey("id") && props.ContainsKey("manufacturer") && props.ContainsKey("model"))
            {
                var existingInstance = _discoveredInstances.FirstOrDefault(x => x.Id == props["id"]);
                if (existingInstance == null)
                {
                    _discoveredInstances.Add(
                        new DiscoveredValetudoInstance(
                            props["id"],
                            $"{props["manufacturer"]} {props["model"]} ({props["id"]})",
                            zeroconfHost.IPAddress
                        )
                    );
                }
                else
                {
                    existingInstance.LastSeen = DateTime.Now;
                }
            }
        }
        
        _discoveredInstances = _discoveredInstances.OrderBy(x => x.Id).ToList();
        UpdateIconState();
    }
    
    // adapted from https://stackoverflow.com/a/43232486
    private static void OpenUrl(string url)
    {
        url = url.Replace("&", "^&");
        // hack because of this: https://github.com/dotnet/corefx/issues/10361
        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (OperatingSystem.IsLinux())
            Process.Start("xdg-open", url);
        else if (OperatingSystem.IsMacOS())
            Process.Start("open", url);
        else
            throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
    }
}