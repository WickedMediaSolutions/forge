using System.Collections.ObjectModel;

namespace EvenniaAtlas.Models;

/// <summary>Full data model for game items.</summary>
public class ItemModel
{
    // Identity
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public EvenniaObjectMetadata Metadata { get; set; } = new();

    // Classification
    public ItemType ItemType { get; set; } = ItemType.Generic;
    public EquipmentSlot EquipmentSlot { get; set; } = EquipmentSlot.None;

    // Numeric attributes
    public int LevelRequirement { get; set; }
    public double Encumbrance { get; set; }
    public int ItemLimit { get; set; }
    public decimal BaseValue { get; set; }

    // Flags
    public bool IsGettable { get; set; } = true;
    public bool IsDroppable { get; set; } = true;
    public bool IsSellable { get; set; } = true;
    public bool IsTradeable { get; set; } = true;
    public bool IsUnique { get; set; }
    public bool IsQuestItem { get; set; }
    public bool IsMagical { get; set; }
    public int MagicLevel { get; set; }

    // Type-specific data (nullable)
    public WeaponDataModel? WeaponData { get; set; }
    public ArmorDataModel? ArmorData { get; set; }

    // Collections
    public ObservableCollection<ItemRequirementModel> Requirements { get; set; } = new();
    public ObservableCollection<ItemModifierModel> Modifiers { get; set; } = new();
    public ObservableCollection<ItemEffectModel> Effects { get; set; } = new();
    public ObservableCollection<ItemSourceModel> Sources { get; set; } = new();
    public ObservableCollection<CraftingComponentModel> CraftingComponents { get; set; } = new();
}