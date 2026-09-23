namespace EvenniaAtlas.Models;

public class DialogueResponseModel
{
    public string Text { get; set; } = string.Empty;

    public string NextNodeId { get; set; } = string.Empty;

    public string StartsQuestId { get; set; } = string.Empty;

    public string CompletesQuestId { get; set; } = string.Empty;
}