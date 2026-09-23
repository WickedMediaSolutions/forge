using System.Collections.ObjectModel;

namespace EvenniaAtlas.Models;

/// <summary>Reusable Evennia object metadata shared by rooms, exits, items, NPCs, etc.</summary>
public class EvenniaObjectMetadata
{
    public string TypeclassPath { get; set; } = string.Empty;
    public ObservableCollection<AliasModel> Aliases { get; set; } = new();
    public ObservableCollection<TagModel> Tags { get; set; } = new();
    public ObservableCollection<AttributeModel> Attributes { get; set; } = new();
    public ObservableCollection<string> Permissions { get; set; } = new();
    public string LockString { get; set; } = string.Empty;
}