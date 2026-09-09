using Epoch.Core.Domain;
using Epoch.Core.Numerics;

namespace Epoch.Core.Effects
{
    /// <summary>
    /// Core Spec sec.5.13. Magnitude is authored content; the ordering rule is
    /// [Final Lock 4]. Only ADD and MULTIPLY exist.
    /// </summary>
    public sealed record ActiveEffect(
        StableId EffectId,
        EffectSourceType SourceType,
        StableId SourceId,
        EffectTrigger Trigger,
        EffectScope Scope,
        string? ScopeValue,
        EffectOperation Operation,
        string TargetStat,
        FixedValue Magnitude,
        int Priority,
        int AppliesFrom)
    {
        public const int DefaultAddPriority = 100;

        public const int DefaultMultiplyPriority = 200;
    }

    /// <summary>What is being asked for: a target stat in a given scope at a given trigger.</summary>
    public sealed record EffectQuery(
        string TargetStat,
        EffectTrigger Trigger,
        Side Owner,
        LaneId? Lane,
        UnitClass? UnitClass,
        FixedValue BaseValue);

    /// <summary>The set of effects in force for a side this turn. Assembled in M1/M2.</summary>
    public sealed record EffectContext(Side Owner, int Turn);
}
