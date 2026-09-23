namespace EvenniaAtlas.Models;

/// <summary>Armor-specific statistics.</summary>
public class ArmorDataModel
{
    public ArmorType ArmorType { get; set; }
    public double ArmorClass { get; set; }
    public double DamageReduction { get; set; }
    public double AccuracyModifier { get; set; }
}