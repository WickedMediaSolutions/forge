using System.Text.Json.Serialization;

namespace EvenniaMapMaker.Models;

public class ConnectionModel
{
    public string Id { get; set; } = string.Empty;
    public string SourceRoomId { get; set; } = string.Empty;
    public string DestinationRoomId { get; set; } = string.Empty;
    public Direction Direction { get; set; }
    public Direction ReverseDirection { get; set; }
    public bool IsOneWay { get; set; }
    public ExitType ExitType { get; set; } = ExitType.Normal;
    public string Aliases { get; set; } = string.Empty;
    public string SharedDoorId { get; set; } = string.Empty;
    public DoorModel? Door { get; set; }
    public bool AutoCreateReverse { get; set; } = true;

    [JsonIgnore]
    public bool HasDoor => ExitType == ExitType.Door && Door != null;
}