namespace EvenniaAtlas.Models;

/// <summary>Trigger condition for an item effect.</summary>
public enum ItemEffectTrigger
{
    Use,
    Equip,
    Unequip,
    Hit,
    BeingHit,
    Consume,
    Read,
    Open,
    Passive
}