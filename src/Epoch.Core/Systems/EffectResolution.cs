using System.Collections.Generic;
using Epoch.Core.Domain;
using Epoch.Core.Effects;
using Epoch.Core.Numerics;

namespace Epoch.Core.Systems
{
    /// <summary>
    /// Core Spec sec.5.13 ordering rule, [Final Lock 4].
    ///
    /// Collect applicable effects, then partition by operation so that **every ADD
    /// resolves before every MULTIPLY** - even an ADD with a numerically higher priority
    /// than a MULTIPLY (GT-27). Within each partition sort by ascending priority, then
    /// sourceType in declaration order (LANE_MODIFIER, STRUCTURE, PERK, COMMANDER), then
    /// ascending ordinal sourceId. Resolve ADD, resolve MULTIPLY, clamp, then round once.
    ///
    /// Identical inputs therefore produce an identical total order on every platform.
    ///
    /// <para>This class owns ordering and arithmetic only. Deciding <i>which</i> effects
    /// apply belongs to <see cref="EffectSources"/>, because the answer depends on where
    /// the effect is being applied - a UNIT_CLASS Power bonus and a LANE Power bonus mean
    /// different things even though they name the same target stat. Keeping one scope
    /// implementation rather than two is what stops an authored effect from quietly
    /// matching nothing.</para>
    /// </summary>
    public static class EffectResolution
    {
        /// <summary>
        /// Apply the total order to a base value. <paramref name="applicable"/> must
        /// already be gathered by <see cref="EffectSources"/> for the application point.
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
        /// The total order within one operation partition. List.Sort is not a stable sort,
        /// which is why the comparator must be total: it falls through priority, source
        /// type and source id to the effect id, and effect ids are unique across the pool.
        ///
        /// Two instances of the same structure card can present the same effect twice.
        /// They compare equal, and that is harmless: both partitions are commutative in
        /// their own operation, so an arbitrary order between two identical effects
        /// produces an identical result.
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
    }
}
