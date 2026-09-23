namespace EvenniaAtlas.Models;

/// <summary>Canonical intrinsic combat profile for NPCs and monsters.
/// These values represent natural attacks and defenses independent of equipped items.</summary>
public class NpcCombatModel
{
    public int DamageMin { get; set; } = 0;
    public int DamageMax { get; set; } = 0;

    public double AccuracyModifier { get; set; } = 0;
    public double AttackSpeed { get; set; } = 1.0;

    public double ArmorClass { get; set; } = 0;
    public double DamageReduction { get; set; } = 0;
}