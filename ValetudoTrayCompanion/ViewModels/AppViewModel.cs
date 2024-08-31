using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValetudoTrayCompanion.Models;
using Zeroconf;

namespace ValetudoTrayCompanion.ViewModels;

public partial class AppViewModel : ObservableObject
{
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly CancellationTokenSource _cts = new();

    public AppViewModel(IClassicDesktopStyleApplicationLifetime desktop)
    {
        _desktop = desktop;
        ToolTipText = Constants.DiscoveringInstances;
        StartDiscoveryLoop();
    }

    [ObservableProperty]
    private string _toolTipText;
    
    [ObservableProperty]
    private ObservableCollection<DiscoveredValetudoInstance> _discoveredInstances = new();
    
    private async void StartDiscoveryLoop()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            await DiscoverInstances();
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                var now = DateTime.Now;
                var oldInstances = DiscoveredInstances.Where(x => (now - x.LastSeen).TotalMinutes <= 5);
                foreach (var oldInstance in oldInstances)
                {
                    DiscoveredInstances.Remove(oldInstance);
                }
                
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
                var existingInstance = DiscoveredInstances.FirstOrDefault(x => x.Id == props["id"]);
                if (existingInstance == null)
                {
                    DiscoveredInstances.Add(new DiscoveredValetudoInstance
                        {
                            Id = props["id"],
                            FriendlyName = $"{props["manufacturer"]} {props["model"]} ({props["id"]})",
                            Address = zeroconfHost.IPAddress
                        }
                    );
                }
                else
                {
                    existingInstance.LastSeen = DateTime.Now;
                }
            }
        }
        
        ToolTipText = DiscoveredInstances.Count switch
        {
            > 1 => string.Format(Constants.MultipleInstanceDiscovered, DiscoveredInstances.Count),
            > 0 => string.Format(Constants.SingleInstanceDiscovered, DiscoveredInstances.Count),
            _ => Constants.DiscoveringInstances
        };
    }
    
    [RelayCommand]
    private void Exit()
    {
        _cts.Cancel();
        _desktop.Shutdown();
    }
}