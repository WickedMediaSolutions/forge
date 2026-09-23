namespace EvenniaAtlas.Models;

public class QuestObjectiveModel
{
    public QuestObjectiveType ObjectiveType { get; set; }

    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;

    public int RequiredCount { get; set; } = 1;

    public string Description { get; set; } = string.Empty;
}