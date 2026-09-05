using System.Text.Json.Serialization;

namespace EvenniaAtlas.Models;

public class DoorModel
{
    public string Name { get; set; } = string.Empty;
    public bool StartsOpen { get; set; }
    public bool StartsClosed { get; set; } = true;
    public bool StartsLocked { get; set; }
    public string KeyId { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsOpenable => !StartsLocked;
}