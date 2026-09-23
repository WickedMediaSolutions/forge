using System.Collections.ObjectModel;

namespace EvenniaAtlas.Models;

public class DialogueModel
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public EvenniaObjectMetadata Metadata { get; set; } = new();

    public string StartNodeId { get; set; } = string.Empty;

    public ObservableCollection<DialogueNodeModel> Nodes { get; set; } = new();
}