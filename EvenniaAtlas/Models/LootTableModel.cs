using System.Collections.ObjectModel;

namespace EvenniaAtlas.Models;

public class LootTableModel
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public EvenniaObjectMetadata Metadata { get; set; } = new();

    public ObservableCollection<LootEntryModel> Entries { get; set; } = new();
}