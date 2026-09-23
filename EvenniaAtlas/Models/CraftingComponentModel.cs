namespace EvenniaAtlas.Models;

/// <summary>Component used in crafting recipes.</summary>
public class CraftingComponentModel
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}