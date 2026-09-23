namespace EvenniaAtlas.Models;

/// <summary>Single waypoint within an NPC patrol route.
/// Data-only configuration — no runtime pathfinding.</summary>
public class NpcPatrolWaypointModel
{
    /// <summary>Canonical reference to RoomModel.Id.</summary>
    public string RoomId { get; set; } = string.Empty;

    /// <summary>Denormalized editor-friendly display name (RoomModel.Title).</summary>
    public string RoomName { get; set; } = string.Empty;

    /// <summary>Configured wait duration at this waypoint in seconds.</summary>
    public double WaitSeconds { get; set; } = 0;
}