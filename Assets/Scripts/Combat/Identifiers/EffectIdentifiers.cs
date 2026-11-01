using System;

namespace MutaBorn.Gameplay
{
    public enum EffectTrigger
    {
        None = 0,
        OnActionUsed = 1,
        OnDamageDealt = 2,
        OnDamageReceived = 3,
        OnHealthRestored = 4,
        OnKill = 5,
        OnDefeated = 6,
        OnTurnStart = 7,
        OnTurnEnd = 8,
        OnSourceActivated = 9,
        OnSourceRemoved = 10,
        OnEnterArea = 11,
        OnLeaveArea = 12

    }

    public enum EffectOperation
    {
        None = 0,
        DealDamage = 1,
        ModifyResource = 2,
        ApplyStatModifier = 3,
        RemoveStatModifiers = 4,
        ApplyStatus = 5,
        RemoveStatus = 6

    }

    // Where to begin finding recipients. Owner is the unit that holds
    // the effect, while EventSource is the actor that caused the event.
    public enum EffectTargetReference
    {
        Owner = 0,
        ActionTarget = 1,
        EventSource = 2,
        EventTarget = 3,
        TargetPoint = 4

    }

    public enum EffectTargetSelection
    {
        Single = 0,
        AllInRadius = 1,
        Nearest = 2,
        RandomOne = 3

    }

    [Flags]
    public enum EffectFactionMask
    {
        None = 0,
        Allied = 1 << 0,
        Neutral = 1 << 1,
        Hostile = 1 << 2,
        Any = Allied | Neutral | Hostile

    }

    public enum EffectProcFrequency
    {
        OncePerResolution = 0,
        OncePerTarget = 1,
        OncePerTriggerEvent = 2

    }

    public enum EffectResolutionPhase
    {
        PreResolution = 0,
        Primary = 1,
        Reaction = 2,
        Aftereffect = 3

    }

    public enum EffectLifetime
    {
        Instant = 0,
        WhileSourceActive = 1,
        TurnCount = 2,
        LimitedUses = 3,
        UntilRemoved = 4

    }

    public enum EffectSourceType
    {
        Unspecified = 0,
        Action = 1,
        Mutation = 2,
        Equipment = 3,
        Consumable = 4,
        Status = 5,
        Terrain = 6,
        Environment = 7

    }

}
