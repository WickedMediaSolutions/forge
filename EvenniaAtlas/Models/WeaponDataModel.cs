namespace EvenniaAtlas.Models;

/// <summary>Weapon-specific statistics and requirements.</summary>
public class WeaponDataModel
{
    public WeaponType WeaponType { get; set; }
    public int DamageMin { get; set; }
    public int DamageMax { get; set; }
    public int StrengthRequirement { get; set; }
    public double AccuracyModifier { get; set; }
    public double BackstabAccuracyModifier { get; set; }
    public double Speed { get; set; }
    public int Range { get; set; }
    public int HandsRequired { get; set; } = 1;
}