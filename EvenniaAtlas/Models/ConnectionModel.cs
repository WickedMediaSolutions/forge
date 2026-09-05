using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace EvenniaAtlas.Models;

public class ConnectionModel
{
    // Atlas core fields
    public string Id { get; set; } = string.Empty;
    public string SourceRoomId { get; set; } = string.Empty;
    public string DestinationRoomId { get; set; } = string.Empty;
    public Direction Direction { get; set; }
    public Direction ReverseDirection { get; set; }
    public bool IsOneWay { get; set; }
    public ExitType ExitType { get; set; } = ExitType.Normal;
    public string Aliases { get; set; } = string.Empty;        // Atlas convenience (comma-separated)
    public string SharedDoorId { get; set; } = string.Empty;
    public DoorModel? Door { get; set; }
    public bool AutoCreateReverse { get; set; } = true;

    // Evennia-native metadata
    public string KeyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TypeclassPath { get; set; } = string.Empty;
    public ObservableCollection<AliasModel> AliasesList { get; set; } = new();
    public ObservableCollection<TagModel> EvenniaTags { get; set; } = new();
    public ObservableCollection<AttributeModel> Attributes { get; set; } = new();
    public string LockString { get; set; } = string.Empty;
    public ObservableCollection<string> Permissions { get; set; } = new();

    [JsonIgnore]
    public bool HasDoor => ExitType == ExitType.Door && Door != null;
}