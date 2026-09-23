namespace EvenniaAtlas.Models;

/// <summary>Stat modifier applied by an item.</summary>
public class ItemModifierModel
{
    public string ModifierType { get; set; } = string.Empty;
    public double Value { get; set; }
    public ModifierValueType ValueType { get; set; }
    public string Notes { get; set; } = string.Empty;
}