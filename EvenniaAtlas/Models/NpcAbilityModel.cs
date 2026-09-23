namespace EvenniaAtlas.Models;

public class NpcAbilityModel
{
    public string AbilityId { get; set; } = string.Empty;
    public string AbilityName { get; set; } = string.Empty;
    public double ChancePercent { get; set; } = 100.0;
    public double CooldownSeconds { get; set; } = 0;
    public int Priority { get; set; } = 0;
    public string Notes { get; set; } = string.Empty;
}
