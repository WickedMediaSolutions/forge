namespace EvenniaMapMaker.Models;

public class RoomModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Notes { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? Id : Title;

    [System.Text.Json.Serialization.JsonIgnore]
    public string TagsString
    {
        get => string.Join(", ", Tags);
        set => Tags = string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}