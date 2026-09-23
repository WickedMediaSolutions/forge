using System.Collections.ObjectModel;

namespace EvenniaAtlas.Models;

/// <summary>Owned NPC patrol-route configuration.
/// Data-only — no runtime movement execution.</summary>
public class NpcPatrolModel
{
    /// <summary>Whether this NPC's patrol configuration is enabled.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Whether the route repeats after the final waypoint.</summary>
    public bool Loop { get; set; } = true;

    /// <summary>Ordered waypoint sequence. Collection order IS patrol order.</summary>
    public ObservableCollection<NpcPatrolWaypointModel> Waypoints { get; set; } = new();
}