using System.Diagnostics;
using System.Runtime.InteropServices;
using Zeroconf;

namespace valetudo_tray_companion;

public class MainApplicationContext : ApplicationContext
{
    private NotifyIcon Icon { get; set; }
    private List<DiscoveredValetudoInstance> _discoveredInstances;
    private CancellationTokenSource Cts { get; set; }
    
    public MainApplicationContext()
    {
        Icon = new NotifyIcon();
        _discoveredInstances = new List<DiscoveredValetudoInstance>();
        Cts = new CancellationTokenSource();
        
        InitIcon();
        UpdateIconState();
        StartDiscoveryLoop();
    }

    private void InitIcon()
    {
        var exe = System.Reflection.Assembly.GetExecutingAssembly();
        var iconStream = exe.GetManifestResourceStream("valetudo_tray_companion.res.logo.ico");
        Icon.Icon = iconStream != null ? new Icon(iconStream) : new Icon(SystemIcons.Application, 40, 40);

        Icon.Visible = true;
    }

    private void UpdateIconState()
    {
        Icon.Text = _discoveredInstances.Count switch
        {
            > 1 => _discoveredInstances.Count + " instances discovered",
            > 0 => _discoveredInstances.Count + " instance discovered",
            _ => "Discovering instances..."
        };

        var ctxMenu = new ContextMenuStrip();

        Icon.ContextMenuStrip = ctxMenu;

        foreach (var discoveredValetudoInstance in _discoveredInstances)
        {
            var item = ctxMenu.Items.Add(discoveredValetudoInstance.FriendlyName);

            item.Click += (_, _) =>
            {
                OpenUrl("http://" + discoveredValetudoInstance.Address);
            };
        }

        ctxMenu.Items.Add(new ToolStripSeparator());
        var closeItem = ctxMenu.Items.Add("Exit");

        closeItem.Click += (_, _) =>
        {
            Cts.Cancel();
            Application.Exit();
        };
    }



    private async void StartDiscoveryLoop()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        
        try
        {
            await DiscoverInstances();
            while (await timer.WaitForNextTickAsync(Cts.Token))
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
        IReadOnlyList<IZeroconfHost> results = await ZeroconfResolver.ResolveAsync("_valetudo._tcp.local.", cancellationToken: Cts.Token);

        foreach (var zeroconfHost in results)
        {
            Console.WriteLine(zeroconfHost.Id);
            

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

                if (props.ContainsKey("id"))
                {
                    var existingInstance = _discoveredInstances.FirstOrDefault(x => x.Id == props["id"]);
                    if (existingInstance == null)
                    {
                        _discoveredInstances.Add(
                            new DiscoveredValetudoInstance(
                                props["id"],
                                zeroconfHost.DisplayName + " (" + props["id"] + ")",
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