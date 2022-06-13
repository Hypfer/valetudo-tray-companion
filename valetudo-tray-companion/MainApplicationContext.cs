using System.Diagnostics;
using System.Runtime.InteropServices;
using Zeroconf;

namespace valetudo_tray_companion;

public class MainApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly List<ToolStripItem> _utilityToolStripItems;
    private readonly CancellationTokenSource _cts;
    private readonly AutostartManager _autostartManager;
    private List<DiscoveredValetudoInstance> _discoveredInstances;
    
    public MainApplicationContext()
    {
        _icon = new NotifyIcon();
        _utilityToolStripItems = new List<ToolStripItem>();
        _cts = new CancellationTokenSource();
        _autostartManager = new AutostartManager();
        _discoveredInstances = new List<DiscoveredValetudoInstance>();

        InitIcon();
        UpdateIconState();
        StartDiscoveryLoop();
    }

    private void InitIcon()
    {
        var exe = System.Reflection.Assembly.GetExecutingAssembly();
        var iconStream = exe.GetManifestResourceStream("valetudo_tray_companion.res.logo.ico");
        _icon.Icon = iconStream != null ? new Icon(iconStream) : new Icon(SystemIcons.Application, 40, 40);
        _icon.Visible = true;
        
        
        _utilityToolStripItems.Add(new ToolStripSeparator());
        
        if (_autostartManager.IsReady())
        {
            var autoStartItem = new ToolStripMenuItem("Run on startup");
            autoStartItem.Checked = _autostartManager.IsAutostartEnabled();

            autoStartItem.Click += (_, _) =>
            {
                if (_autostartManager.IsAutostartEnabled())
                {
                    _autostartManager.DisableAutostart();
                }
                else
                {
                    _autostartManager.EnableAutostart();
                }
                
                autoStartItem.Checked = _autostartManager.IsAutostartEnabled();
            };
        
            _utilityToolStripItems.Add(autoStartItem);
        }
        
        var closeItem = new ToolStripMenuItem("Exit");
        closeItem.Click += (_, _) =>
        {
            _cts.Cancel();
            Application.Exit();
        };

        _utilityToolStripItems.Add(closeItem);
    }

    private void UpdateIconState()
    {
        _icon.Text = _discoveredInstances.Count switch
        {
            > 1 => _discoveredInstances.Count + " instances discovered",
            > 0 => _discoveredInstances.Count + " instance discovered",
            _ => "Discovering instances..."
        };

        var ctxMenu = new ContextMenuStrip();

        _icon.ContextMenuStrip = ctxMenu;

        foreach (var discoveredValetudoInstance in _discoveredInstances)
        {
            var item = ctxMenu.Items.Add(discoveredValetudoInstance.FriendlyName);

            item.Click += (_, _) =>
            {
                OpenUrl("http://" + discoveredValetudoInstance.Address);
            };
        }

        foreach (var utilityToolStripItem in _utilityToolStripItems)
        {
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
        } catch(OperationCanceledException)
        {
            //intentional
        }
    }

    private async Task DiscoverInstances()
    {
        IReadOnlyList<IZeroconfHost> results = await ZeroconfResolver.ResolveAsync("_valetudo._tcp.local.", cancellationToken: _cts.Token);

        foreach (var zeroconfHost in results)
        {
            if (zeroconfHost.Services.Count > 0)
            {
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
            
        }

        _discoveredInstances = _discoveredInstances.OrderBy(x => x.Id).ToList();

        UpdateIconState();
    }


    // adapted from https://stackoverflow.com/a/43232486
    private static void OpenUrl(string url)
    {
        // hack because of this: https://github.com/dotnet/corefx/issues/10361
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}