using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace EvenniaAtlas.Models;

public class DoorModel
{
    // Atlas door convenience fields
    public string Name { get; set; } = string.Empty;
    public string Typeclass { get; set; } = string.Empty;
    public bool StartsOpen { get; set; }
    public bool StartsClosed { get; set; } = true;
    public bool StartsLocked { get; set; }
    public bool Lockable { get; set; } = true;
    public string KeyId { get; set; } = string.Empty;
    public bool SynchronizeOpposite { get; set; } = true;

    // Access configuration
    public string TraverseLockString { get; set; } = string.Empty;
    public string LockedFailureMessage { get; set; } = string.Empty;
    public string ClosedFailureMessage { get; set; } = string.Empty;

    // Door-specific metadata (separate from exit Evennia metadata)
    public string Description { get; set; } = string.Empty;
    public ObservableCollection<AliasModel> DoorAliases { get; set; } = new();
    public ObservableCollection<TagModel> DoorTags { get; set; } = new();
    public ObservableCollection<AttributeModel> DoorAttributes { get; set; } = new();
    public ObservableCollection<string> DoorPermissions { get; set; } = new();

    [JsonIgnore]
    public bool IsOpenable => !StartsLocked;
}