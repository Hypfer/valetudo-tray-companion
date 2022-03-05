namespace valetudo_tray_companion;

public class DiscoveredValetudoInstance
{
    public readonly string Id;
    public readonly string FriendlyName;
    public readonly string Address;
    public DateTime LastSeen;

    public DiscoveredValetudoInstance(string id, string friendlyName, string address)
    {
        this.Id = id;
        this.FriendlyName = friendlyName;
        this.Address = address;

        this.LastSeen = DateTime.Now;
    }
}