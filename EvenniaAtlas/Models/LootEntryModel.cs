namespace EvenniaAtlas.Models;

public class LootEntryModel
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;

    public double ChancePercent { get; set; }

    public int QuantityMin { get; set; } = 1;
    public int QuantityMax { get; set; } = 1;

    public bool IsEnabled { get; set; } = true;
}