using System.Collections.ObjectModel;

namespace EvenniaAtlas.Models;

/// <summary>Foundation data model for game NPCs/mobs.</summary>
public class NpcModel
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public EvenniaObjectMetadata Metadata { get; set; } = new();
    public string LootTableId { get; set; } = string.Empty;
    public string DialogueId { get; set; } = string.Empty;
    public NpcStatsModel Stats { get; set; } = new();
    public NpcCombatModel Combat { get; set; } = new();
    public NpcClassificationModel Classification { get; set; } = new();
    public NpcBehaviorModel Behavior { get; set; } = new();
    public ObservableCollection<NpcAbilityModel> Abilities { get; set; } = new();
    public ObservableCollection<NpcResistanceModel> Resistances { get; set; } = new();
    public ObservableCollection<NpcEquipmentModel> Equipment { get; set; } = new();
    public ObservableCollection<NpcInventoryItemModel> Inventory { get; set; } = new();
    public NpcPatrolModel Patrol { get; set; } = new();
}