namespace EvenniaAtlas.Models;

/// <summary>Canonical gameplay classification for NPCs and monsters.
/// Values are string identifiers for species, profession, faction, and alignment.
/// Registries and enums are not yet implemented — these are raw identifiers only.</summary>
public class NpcClassificationModel
{
    public string Species { get; set; } = string.Empty;
    public string Profession { get; set; } = string.Empty;
    public string Faction { get; set; } = string.Empty;
    public string Alignment { get; set; } = string.Empty;
}