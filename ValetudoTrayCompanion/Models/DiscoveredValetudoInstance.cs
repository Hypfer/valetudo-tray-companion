using System;

namespace ValetudoTrayCompanion.Models;

public sealed class DiscoveredValetudoInstance
{
    public readonly string Id;
    public readonly string FriendlyName;
    public readonly string Address;
    public DateTime LastSeen;

    public DiscoveredValetudoInstance(string id, string friendlyName, string address)
    {
        Id = id;
        FriendlyName = friendlyName;
        Address = address;
        LastSeen = DateTime.Now;
    }
}