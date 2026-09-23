namespace EvenniaAtlas.Models;

/// <summary>Source describing where and how an item can be obtained.</summary>
public class ItemSourceModel
{
    public ItemSourceType SourceType { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public double ChancePercent { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal Cost { get; set; }
    public string Notes { get; set; } = string.Empty;
}