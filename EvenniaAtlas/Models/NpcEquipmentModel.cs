namespace EvenniaAtlas.Models;

/// <summary>NPC equipment reference linking an ItemModel to an equipment slot.</summary>
public class NpcEquipmentModel
{
    /// <summary>Canonical reference to ItemModel.Id.</summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>Denormalized editor-friendly display name.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Equipment slot occupied by this NPC equipment entry.</summary>
    public EquipmentSlot Slot { get; set; } = EquipmentSlot.None;
}