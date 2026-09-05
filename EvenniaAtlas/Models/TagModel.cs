namespace EvenniaAtlas.Models;

/// <summary>Structured Evennia-native tag with key, category, and optional data.</summary>
public class TagModel
{
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}