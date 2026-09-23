namespace EvenniaAtlas.Models;

/// <summary>Canonical foundational gameplay stats for NPCs and monsters.</summary>
public class NpcStatsModel
{
    public int Level { get; set; } = 1;

    public int MaxHealth { get; set; } = 1;
    public int MaxMana { get; set; } = 0;
    public int MaxStamina { get; set; } = 0;

    public int Strength { get; set; } = 0;
    public int Agility { get; set; } = 0;
    public int Intellect { get; set; } = 0;
    public int Wisdom { get; set; } = 0;
    public int Charm { get; set; } = 0;

    public int ExperienceReward { get; set; } = 0;

    public decimal CurrencyReward { get; set; } = 0;
}