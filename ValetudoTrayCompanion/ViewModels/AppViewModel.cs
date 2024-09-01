using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using ValetudoTrayCompanion.Models;
using Zeroconf;

namespace ValetudoTrayCompanion.ViewModels;

public partial class AppViewModel
{
    private readonly IClassicDesktopStyleApplicationLifetime _desktopLifetime;
    private readonly TrayIcon _icon = new();
    private readonly List<NativeMenuItemBase> _controlItems = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly AutostartManager _autostartManager = new();
    private List<DiscoveredValetudoInstance> _discoveredInstances = new();

    public AppViewModel(IClassicDesktopStyleApplicationLifetime desktopLifetime)
    {
        _desktopLifetime = desktopLifetime;
        InitIcon();
        UpdateIconState();
        StartDiscoveryLoop();
    }

     private void InitIcon()
     {
         using var valetudoLogoStream = AssetLoader.Open(new Uri("resm:ValetudoTrayCompanion.Assets.logo.ico"));
        _icon.Icon = new WindowIcon(valetudoLogoStream);
        _icon.IsVisible = true;
        _icon.Menu = new NativeMenu();
        
        if (_autostartManager is { IsSupported: true, IsReady: true })
        {
            var autoStartItem = new NativeMenuItem
            {
                Header = "Run on startup",
                ToggleType = NativeMenuItemToggleType.CheckBox,
                IsChecked = _autostartManager.IsAutostartEnabled
            };
            
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
            };
            
            _controlItems.Add(autoStartItem);
        }
        
        var closeItem = new NativeMenuItem("Exit");
        closeItem.Click += (_, _) =>
        {
            _cts.Cancel();
            _desktopLifetime.Shutdown();
        };
        
        _controlItems.Add(closeItem);
     }
    
    
    private void UpdateIconState()
    {
        _icon.ToolTipText = _discoveredInstances.Count switch
        {
            > 1 => $"{_discoveredInstances.Count} instances discovered",
            > 0 => $"{_discoveredInstances.Count} instance discovered",
            _ => "Discovering instances..."
        };
        
        _icon.Menu!.Items.Clear();
        
        foreach (var discoveredValetudoInstance in _discoveredInstances)
        {
            var item = new NativeMenuItem(discoveredValetudoInstance.FriendlyName);
            _icon.Menu!.Items.Add(item);

            item.Click += (_, _) =>
            {
                OpenUrl("http://" + discoveredValetudoInstance.Address);
            };
        }

        if (_discoveredInstances.Count > 0)
        {
            _icon.Menu.Items.Add(new NativeMenuItemSeparator());
        }
        
        foreach (var controlItem in _controlItems)
        {
            _icon.Menu.Items.Add(controlItem);
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