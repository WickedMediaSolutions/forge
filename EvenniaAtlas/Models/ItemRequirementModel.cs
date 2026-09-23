namespace EvenniaAtlas.Models;

/// <summary>Requirement that must be met to equip or use an item.</summary>
public class ItemRequirementModel
{
    public RequirementType RequirementType { get; set; }
    public ComparisonOperator Operator { get; set; }
    public string Value { get; set; } = string.Empty;
}