namespace EvenniaAtlas.Models;

/// <summary>Structured Evennia-native alias with optional category.</summary>
public class AliasModel
{
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}