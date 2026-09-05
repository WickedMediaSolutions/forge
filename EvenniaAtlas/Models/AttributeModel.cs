namespace EvenniaAtlas.Models;

/// <summary>Evennia-native attribute with key, value, category, and lockstring.</summary>
public class AttributeModel
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string LockString { get; set; } = string.Empty;
}