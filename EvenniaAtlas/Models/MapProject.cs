namespace EvenniaAtlas.Models;

public class MapProject
{
    public int Version { get; set; } = 1;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DefaultRoomTitle { get; set; } = string.Empty;
    public List<RoomModel> Rooms { get; set; } = new();
    public List<ConnectionModel> Connections { get; set; } = new();
}