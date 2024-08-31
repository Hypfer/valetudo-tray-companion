using System;

namespace ValetudoTrayCompanion.Models;

public sealed record DiscoveredValetudoInstance
{
    public required string Id { get; init; }
    public required string FriendlyName { get; init; }
    public required string Address { get; init; }
    public DateTime LastSeen { get; set; } = DateTime.Now;
}