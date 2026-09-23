using System.Collections.ObjectModel;

namespace EvenniaAtlas.Models;

public class QuestModel
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public EvenniaObjectMetadata Metadata { get; set; } = new();

    public string GiverNpcId { get; set; } = string.Empty;
    public string TurnInNpcId { get; set; } = string.Empty;

    public ObservableCollection<QuestObjectiveModel> Objectives { get; set; } = new();

    public int ExperienceReward { get; set; }
    public decimal CurrencyReward { get; set; }

    public ObservableCollection<QuestItemRewardModel> ItemRewards { get; set; } = new();

    public string RewardLootTableId { get; set; } = string.Empty;
}