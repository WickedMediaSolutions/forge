namespace EvenniaAtlas.Models;

/// <summary>Effect that an item can trigger under specific conditions.</summary>
public class ItemEffectModel
{
    public string EffectType { get; set; } = string.Empty;
    public string EffectId { get; set; } = string.Empty;
    public ItemEffectTrigger Trigger { get; set; }
    public int Charges { get; set; }
    public int UsesPerDay { get; set; }
    public double ChancePercent { get; set; }
    public string Notes { get; set; } = string.Empty;
}