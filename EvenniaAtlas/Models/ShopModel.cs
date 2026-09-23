using System.Collections.ObjectModel;

namespace EvenniaAtlas.Models;

/// <summary>Foundation data model for game shops run by NPCs.</summary>
public class ShopModel
{
    /// <summary>Canonical shop identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>User-facing shop name.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Editor description of the shop.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Free-form authoring notes.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Evennia object metadata (typeclass, aliases, tags, attributes, permissions, lock string).</summary>
    public EvenniaObjectMetadata Metadata { get; set; } = new();

    /// <summary>Canonical reference to the owning NpcModel.Id.</summary>
    public string NpcId { get; set; } = string.Empty;

    /// <summary>Items offered for sale in this shop.</summary>
    public ObservableCollection<ShopInventoryEntryModel> Inventory { get; set; } = new();
}