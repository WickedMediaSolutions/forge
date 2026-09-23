namespace EvenniaAtlas.Models;

/// <summary>NPC inventory entry referencing an ItemModel with a configured quantity.</summary>
public class NpcInventoryItemModel
{
    /// <summary>Canonical reference to ItemModel.Id.</summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>Denormalized editor-friendly display name.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Configured quantity of this item carried by the NPC.</summary>
    public int Quantity { get; set; } = 1;
}