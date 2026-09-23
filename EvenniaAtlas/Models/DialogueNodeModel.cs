using System.Collections.ObjectModel;

namespace EvenniaAtlas.Models;

public class DialogueNodeModel
{
    public string Id { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string SpeakerNpcId { get; set; } = string.Empty;

    public string SpeakerName { get; set; } = string.Empty;

    public ObservableCollection<DialogueResponseModel> Responses { get; set; }
        = new();
}