namespace EvenniaAtlas.Models;

public class NpcResistanceModel
{
    public string DamageType { get; set; } = string.Empty;

    public double ResistancePercent { get; set; } = 0;

    public bool IsImmune { get; set; } = false;

    public string Notes { get; set; } = string.Empty;
}