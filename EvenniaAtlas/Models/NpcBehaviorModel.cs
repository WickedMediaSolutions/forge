namespace EvenniaAtlas.Models;

/// <summary>Editor-side configuration data for basic NPC behavior.
/// All values are data classifications only — no runtime AI, movement, or combat behavior is implemented here.</summary>
public class NpcBehaviorModel
{
    public NpcAggressionMode AggressionMode { get; set; } = NpcAggressionMode.Passive;

    public double AggroRange { get; set; } = 0;

    public bool CanWander { get; set; } = false;

    public double WanderIntervalSeconds { get; set; } = 0;

    public bool CanFlee { get; set; } = false;

    public double FleeHealthPercent { get; set; } = 0;
}