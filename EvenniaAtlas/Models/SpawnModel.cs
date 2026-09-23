namespace EvenniaAtlas.Models;

/// <summary>Foundation data model for spawn points referencing items or NPCs.</summary>
public class SpawnModel
{
    public string Id { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public EntityType EntityType { get; set; }
    public int Quantity { get; set; } = 1;
    public double RespawnSeconds { get; set; }
    public bool Enabled { get; set; } = true;
}