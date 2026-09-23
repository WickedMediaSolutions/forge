namespace EvenniaAtlas.Models;

/// <summary>Canonical intrinsic combat profile for NPCs and monsters.
/// These values represent natural attacks and defenses independent of equipped items.</summary>
public class NpcCombatModel
{
    public int DamageMin { get; set; } = 0;
    public int DamageMax { get; set; } = 0;

    public double AccuracyModifier { get; set; } = 0;
    /// <summary>Combat-rate multiplier. 1.0 = normal/base attack rate.
    /// Values greater than 1.0 attack more frequently.
    /// Values between 0 and 1.0 attack less frequently.
    /// The runtime base interval is controlled by the game/server.
    /// AttackSpeed does not represent seconds.</summary>
    public double AttackSpeed { get; set; } = 1.0;

    public double ArmorClass { get; set; } = 0;
    public double DamageReduction { get; set; } = 0;
}