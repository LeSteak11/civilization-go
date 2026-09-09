using System;
using System.Collections.Generic;
using Epoch.Core.Domain;
using Epoch.Core.Effects;
using Epoch.Core.Numerics;

namespace Epoch.Core.Systems
{
    /// <summary>
    /// The facts an effect's scope is tested against. Assembled by the calling system so
    /// that scope matching stays a pure function of already-authoritative state.
    /// </summary>
    public readonly record struct EffectScopeContext(
        Side Owner,
        LaneId? Lane,
        LaneModifier? LaneModifier,
        UnitClass? UnitClass,
        bool UnitHasReach,
        bool OwnerHoldsContestedTile,
        LaneId? TargetLane);

    /// <summary>
    /// Core Spec sec.5.13 ordering rule, [Final Lock 4].
    ///
    /// Collect applicable effects, then partition by operation so that **every ADD
    /// resolves before every MULTIPLY** — even an ADD with a numerically higher priority
    /// than a MULTIPLY (GT-27). Within each partition sort by ascending priority, then
    /// sourceType in declaration order (LANE_MODIFIER, STRUCTURE, PERK, COMMANDER), then
    /// ascending ordinal sourceId. Resolve ADD, resolve MULTIPLY, clamp, then round once.
    ///
    /// Identical inputs therefore produce an identical total order on every platform.
    /// </summary>
    public static class EffectResolution
    {
        /// <summary>
        /// Does this effect apply in this context? Scope values are the authored
        /// vocabulary of the V1 content manifest.
        /// </summary>
        public static bool Applies(ActiveEffect effect, EffectScopeContext context, int turn)
        {
            if (effect.AppliesFrom > turn)
            {
                // Effects are never retroactive (Core Spec sec.6.4 A4).
                return false;
            }

            switch (effect.Scope)
            {
                case EffectScope.GLOBAL:
                    return true;

                case EffectScope.SIDE:
                    // "OWNER" is the only authored side scope: the acquiring side.
                    return true;

                case EffectScope.LANE:
                    return AppliesToLane(effect.ScopeValue, context);

                case EffectScope.UNIT_CLASS:
                    return AppliesToUnitClass(effect.ScopeValue, context);

                default:
                    return false;
            }
        }

        private static bool AppliesToLane(string? scopeValue, EffectScopeContext context)
        {
            switch (scopeValue)
            {
                case "RIVER":
                    return context.LaneModifier == Domain.LaneModifier.RIVER;
                case "HIGHLAND":
                    return context.LaneModifier == Domain.LaneModifier.HIGHLAND;
                case "COAST":
                    return context.LaneModifier == Domain.LaneModifier.COAST;
                case "OWNER_HOLDS_CONTESTED_TILE":
                    return context.OwnerHoldsContestedTile;
                case "TARGET_LANE":
                    return context.TargetLane is not null && context.Lane == context.TargetLane;
                default:
                    return false;
            }
        }

        private static bool AppliesToUnitClass(string? scopeValue, EffectScopeContext context)
        {
            switch (scopeValue)
            {
                case "SWORD":
                    return context.UnitClass == Domain.UnitClass.SWORD;
                case "SPEAR":
                    return context.UnitClass == Domain.UnitClass.SPEAR;
                case "HORSE":
                    return context.UnitClass == Domain.UnitClass.HORSE;
                case "REACH":
                    return context.UnitHasReach;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Apply the total order to a base value. <paramref name="applicable"/> must
        /// already be scope-filtered; this method owns ordering and arithmetic only.
        /// </summary>
        public static FixedValue Resolve(FixedValue baseValue, IReadOnlyList<ActiveEffect> applicable)
        {
            if (applicable is null || applicable.Count == 0)
            {
                return baseValue;
            }

            List<ActiveEffect> adds = new List<ActiveEffect>();
            List<ActiveEffect> multiplies = new List<ActiveEffect>();
            for (int i = 0; i < applicable.Count; i++)
            {
                if (applicable[i].Operation == EffectOperation.ADD)
                {
                    adds.Add(applicable[i]);
                }
                else
                {
                    multiplies.Add(applicable[i]);
                }
            }

            adds.Sort(CompareEffects);
            multiplies.Sort(CompareEffects);

            FixedValue value = baseValue;
            for (int i = 0; i < adds.Count; i++)
            {
                value += adds[i].Magnitude;
            }

            for (int i = 0; i < multiplies.Count; i++)
            {
                value = value.ScaleByHundredths(multiplies[i].Magnitude.Hundredths);
            }

            return value;
        }

        /// <summary>
        /// The stable total order within one operation partition. A List.Sort is not a
        /// stable sort, which is precisely why the comparator must be total: no two
        /// distinct effects may compare equal, and sourceId uniqueness guarantees that.
        /// </summary>
        public static int CompareEffects(ActiveEffect a, ActiveEffect b)
        {
            int byPriority = a.Priority.CompareTo(b.Priority);
            if (byPriority != 0)
            {
                return byPriority;
            }

            int bySourceType = ((int)a.SourceType).CompareTo((int)b.SourceType);
            if (bySourceType != 0)
            {
                return bySourceType;
            }

            int bySourceId = a.SourceId.CompareTo(b.SourceId);
            return bySourceId != 0 ? bySourceId : a.EffectId.CompareTo(b.EffectId);
        }

        /// <summary>
        /// Gather every effect a side currently owns that matches a stat, trigger and scope.
        /// Perk order follows acquisition order, which is deterministic; the comparator
        /// above then imposes the authoritative order regardless.
        /// </summary>
        public static List<ActiveEffect> Gather(
            PlayerState side,
            string targetStat,
            EffectTrigger trigger,
            EffectScopeContext context,
            int turn)
        {
            List<ActiveEffect> gathered = new List<ActiveEffect>();
            for (int p = 0; p < side.Perks.Count; p++)
            {
                IReadOnlyList<ActiveEffect> effects = side.Perks[p].Effects;
                for (int e = 0; e < effects.Count; e++)
                {
                    ActiveEffect effect = effects[e];
                    if (!string.Equals(effect.TargetStat, targetStat, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (effect.Trigger != trigger)
                    {
                        continue;
                    }

                    if (Applies(effect, context, turn))
                    {
                        gathered.Add(effect);
                    }
                }
            }

            return gathered;
        }
    }
}
