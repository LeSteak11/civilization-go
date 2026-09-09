using System.Collections.Generic;
using Epoch.Core.Domain;
using Epoch.Core.Effects;
using Epoch.Core.Numerics;

namespace Epoch.Core.Systems
{
    /// <summary>
    /// Step 7 of the canonical turn, Core Spec sec.7.1 equations E3-E10.
    ///
    /// Income ticks **after** the card resolves (Q1), so a structure placed this turn
    /// does produce income on the turn it is placed (B8).
    ///
    /// Contested income reads <c>tile3HolderPrevTurn</c>, not this turn's ownership -
    /// ownership is computed once, after combat, at step 12 [Lock 4], [Lock 15]. On turn 1
    /// that value is null for every lane, so there is no contested income on turn 1 (EC-31).
    /// </summary>
    public static class IncomeSystem
    {
        public static IncomeBreakdown Compute(RunState state, Side side, ResourceType resource)
        {
            PlayerState player = state.SideState(side);

            int baseIncome = resource == ResourceType.GROWTH
                ? RunState.BaseIncomeGrowth
                : RunState.BaseIncomeInsight;

            // E5: structure yields, each already carrying its RIVER bonus from placement.
            int structureIncome = 0;
            List<ActiveEffect> structureEffects = EffectSources.StructureYieldEffects(player, resource);
            for (int i = 0; i < player.Structures.Count; i++)
            {
                StructureInstance structure = player.Structures[i];
                if (structure.YieldType != resource)
                {
                    continue;
                }

                structureIncome += structureEffects.Count == 0
                    ? structure.YieldAmount
                    : EffectResolution
                        .Resolve(FixedValue.FromInt(structure.YieldAmount), structureEffects)
                        .RoundHalfAwayFromZeroToInt();
            }

            // E6: +1 per lane this side exclusively held at the end of the previous turn.
            int contestedIncome = 0;
            List<ActiveEffect> contestedEffects = EffectSources.ContestedYieldEffects(player, resource);
            for (int i = 0; i < state.Lanes.Count; i++)
            {
                if (state.Lanes[i].Tile3HolderPrevTurn != side)
                {
                    continue;
                }

                contestedIncome += contestedEffects.Count == 0
                    ? RunState.ContestedTileYield
                    : EffectResolution
                        .Resolve(FixedValue.FromInt(RunState.ContestedTileYield), contestedEffects)
                        .RoundHalfAwayFromZeroToInt();
            }

            // E7: ON_INCOME effects on the per-turn stat itself, side-wide and lane-scoped.
            int subtotal = baseIncome + structureIncome + contestedIncome;
            List<ActiveEffect> perTurnEffects = EffectSources.IncomeEffects(state, player, resource);
            int total = perTurnEffects.Count == 0
                ? subtotal
                : EffectResolution
                    .Resolve(FixedValue.FromInt(subtotal), perTurnEffects)
                    .RoundHalfAwayFromZeroToInt();

            // A conversion perk can subtract income (River Academies trades Growth for
            // Insight), and Invariant PS-1 says a resource is never driven negative. With
            // a base income of 3 Growth / 2 Insight this floor is unreachable in V1
            // content; it is here so that PS-1 holds by construction rather than by
            // arithmetic coincidence, and it never clamps a positive value.
            if (total < 0)
            {
                total = 0;
            }

            return new IncomeBreakdown(baseIncome, structureIncome, contestedIncome, total - subtotal, total);
        }

        /// <summary>Credit both sides. E9, E10.</summary>
        public static RunState ApplyBoth(
            RunState state,
            out IncomeBreakdown playerGrowth,
            out IncomeBreakdown playerInsight,
            out IncomeBreakdown snapshotGrowth,
            out IncomeBreakdown snapshotInsight)
        {
            playerGrowth = Compute(state, Side.PLAYER, ResourceType.GROWTH);
            playerInsight = Compute(state, Side.PLAYER, ResourceType.INSIGHT);
            snapshotGrowth = Compute(state, Side.SNAPSHOT, ResourceType.GROWTH);
            snapshotInsight = Compute(state, Side.SNAPSHOT, ResourceType.INSIGHT);

            PlayerState player = state.Player with
            {
                Growth = state.Player.Growth + playerGrowth.Total,
                Insight = state.Player.Insight + playerInsight.Total,
            };

            PlayerState snapshot = state.Snapshot with
            {
                Growth = state.Snapshot.Growth + snapshotGrowth.Total,
                Insight = state.Snapshot.Insight + snapshotInsight.Total,
            };

            return state with { Player = player, Snapshot = snapshot };
        }
    }

    /// <summary>
    /// The components of one side's income in one resource. Recorded for the replay and
    /// for balance telemetry; the authoritative value is <see cref="Total"/>.
    /// </summary>
    public readonly record struct IncomeBreakdown(int Base, int Structures, int Contested, int Effects, int Total);
}
