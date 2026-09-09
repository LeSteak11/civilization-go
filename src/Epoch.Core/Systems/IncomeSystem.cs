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
    /// Contested income reads <c>tile3HolderPrevTurn</c>, not this turn's ownership —
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
            for (int i = 0; i < player.Structures.Count; i++)
            {
                StructureInstance structure = player.Structures[i];
                if (structure.YieldType == resource)
                {
                    structureIncome += StructureYield(state, player, structure, resource);
                }
            }

            // E6: +1 per lane this side exclusively held at the end of the previous turn.
            int contestedIncome = 0;
            for (int i = 0; i < state.Lanes.Count; i++)
            {
                if (state.Lanes[i].Tile3HolderPrevTurn == side)
                {
                    contestedIncome += ContestedYield(state, player, state.Lanes[i], resource);
                }
            }

            // E7: ON_INCOME perk effects on the per-turn stat itself.
            string stat = resource == ResourceType.GROWTH ? "growthPerTurn" : "insightPerTurn";
            EffectScopeContext context = new EffectScopeContext(
                side, null, null, null, false, HoldsAnyContestedTile(state, side), null);
            List<ActiveEffect> effects =
                EffectResolution.Gather(player, stat, EffectTrigger.ON_INCOME, context, state.Turn);

            int subtotal = baseIncome + structureIncome + contestedIncome;
            FixedValue resolved = EffectResolution.Resolve(FixedValue.FromInt(subtotal), effects);
            int total = resolved.RoundHalfAwayFromZeroToInt();

            return new IncomeBreakdown(baseIncome, structureIncome, contestedIncome, total - subtotal, total);
        }

        private static int StructureYield(RunState state, PlayerState player, StructureInstance structure, ResourceType resource)
        {
            string stat = resource == ResourceType.GROWTH ? "structureGrowthYield" : "structureInsightYield";
            LaneState lane = state.Lane(structure.LaneId);
            EffectScopeContext context = new EffectScopeContext(
                player.Side, lane.Id, lane.Modifier, null, false, lane.Tile3HolderPrevTurn == player.Side, lane.Id);

            List<ActiveEffect> effects =
                EffectResolution.Gather(player, stat, EffectTrigger.ON_INCOME, context, state.Turn);
            if (effects.Count == 0)
            {
                return structure.YieldAmount;
            }

            return EffectResolution
                .Resolve(FixedValue.FromInt(structure.YieldAmount), effects)
                .RoundHalfAwayFromZeroToInt();
        }

        private static int ContestedYield(RunState state, PlayerState player, LaneState lane, ResourceType resource)
        {
            string stat = resource == ResourceType.GROWTH ? "contestedTileGrowthYield" : "contestedTileInsightYield";
            EffectScopeContext context = new EffectScopeContext(
                player.Side, lane.Id, lane.Modifier, null, false, true, lane.Id);

            List<ActiveEffect> effects =
                EffectResolution.Gather(player, stat, EffectTrigger.ON_INCOME, context, state.Turn);
            if (effects.Count == 0)
            {
                return RunState.ContestedTileYield;
            }

            return EffectResolution
                .Resolve(FixedValue.FromInt(RunState.ContestedTileYield), effects)
                .RoundHalfAwayFromZeroToInt();
        }

        private static bool HoldsAnyContestedTile(RunState state, Side side)
        {
            for (int i = 0; i < state.Lanes.Count; i++)
            {
                if (state.Lanes[i].Tile3HolderPrevTurn == side)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Credit both sides. E9, E10.</summary>
        public static RunState ApplyBoth(RunState state, out IncomeBreakdown playerGrowth, out IncomeBreakdown playerInsight, out IncomeBreakdown snapshotGrowth, out IncomeBreakdown snapshotInsight)
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
