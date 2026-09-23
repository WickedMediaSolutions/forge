namespace EvenniaAtlas.Models;

/// <summary>Shop inventory entry referencing an ItemModel with shop-specific pricing and stock configuration.</summary>
public class ShopInventoryEntryModel
{
    /// <summary>Canonical reference to ItemModel.Id.</summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>Denormalized editor-friendly display name.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Finite stock quantity. Ignored when IsUnlimited is true.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>When true, Quantity is ignored and stock is effectively limitless.</summary>
    public bool IsUnlimited { get; set; }

    /// <summary>Price the player pays the shop to acquire this item.</summary>
    public decimal BuyPrice { get; set; }

    /// <summary>Price the shop pays the player to acquire this item from the player.</summary>
    public decimal SellPrice { get; set; }

    /// <summary>Whether this entry is active/visible in the shop.</summary>
    public bool IsEnabled { get; set; } = true;
}