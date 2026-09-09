using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.StateMachine;

namespace Epoch.Testing
{
    /// <summary>
    /// Builds small, explicit authoritative states for the spec's named scenarios.
    ///
    /// Every helper sets state directly rather than playing turns to reach it. A golden
    /// test that had to play twelve turns to arrange a board would be testing the whole
    /// engine on its way to testing one rule, and its failure message would say nothing
    /// about which rule broke.
    /// </summary>
    public static class Scenario
    {
        public const string RulesVersion = "1.2.0-v1-final";

        /// <summary>A run with chosen lane modifiers and no units, structures or perks.</summary>
        public static RunState Board(
            LaneModifier laneA = LaneModifier.RIVER,
            LaneModifier laneB = LaneModifier.HIGHLAND,
            LaneModifier laneC = LaneModifier.COAST,
            int turn = 1)
        {
            LaneState[] lanes =
            {
                LaneState.New(LaneId.A, laneA),
                LaneState.New(LaneId.B, laneB),
                LaneState.New(LaneId.C, laneC),
            };

            return new RunState(
                RulesVersion,
                "1.0.1-v1",
                "sha256:test",
                new SeedState(new SeedCode("TEST"), 1, 2, 3, SeedState.PinnedAlgorithm),
                turn,
                RunState.AgeForTurn(turn),
                RunPhase.RUN_READY,
                lanes,
                PlayerState.NewRun(Side.PLAYER),
                PlayerState.NewRun(Side.SNAPSHOT),
                0,
                0,
                null);
        }

        /// <summary>A plain single-lane board (no modifier effects) for combat vectors.</summary>
        public static RunState PlainBoard(int turn = 1) =>
            Board(LaneModifier.RIVER, LaneModifier.RIVER, LaneModifier.RIVER, turn);

        public static UnitInstance Unit(
            string id,
            Side owner,
            UnitClass unitClass,
            int power,
            LaneId lane,
            int tile,
            int hp = UnitInstance.MaxHp,
            bool reach = false,
            int trainedOnTurn = 1,
            int? guardUsedInEngagement = null) =>
            new UnitInstance(
                new StableId(id),
                owner,
                new StableId("TEST_CARD"),
                unitClass,
                power,
                hp,
                lane,
                tile,
                reach,
                trainedOnTurn,
                guardUsedInEngagement);

        public static RunState WithUnits(this RunState state, params UnitInstance[] units)
        {
            List<UnitInstance> player = new List<UnitInstance>(state.Player.Units);
            List<UnitInstance> snapshot = new List<UnitInstance>(state.Snapshot.Units);

            foreach (UnitInstance unit in units)
            {
                if (unit.Owner == Side.PLAYER)
                {
                    player.Add(unit);
                }
                else
                {
                    snapshot.Add(unit);
                }
            }

            return state with
            {
                Player = state.Player with { Units = player },
                Snapshot = state.Snapshot with { Units = snapshot },
            };
        }

        public static RunState WithPrevHolder(this RunState state, LaneId lane, Side? holder) =>
            state.WithLane(state.Lane(lane) with { Tile3HolderPrevTurn = holder });

        public static RunState WithEngagementId(this RunState state, LaneId lane, int engagementId) =>
            state.WithLane(state.Lane(lane) with { EngagementId = engagementId });

        public static RunState WithResources(this RunState state, Side side, int growth, int insight)
        {
            PlayerState updated = state.SideState(side) with { Growth = growth, Insight = insight };
            return state.WithSideState(updated);
        }

        public static RunState WithStructures(this RunState state, Side side, params StructureInstance[] structures)
        {
            List<StructureInstance> list = new List<StructureInstance>(state.SideState(side).Structures);
            list.AddRange(structures);
            return state.WithSideState(state.SideState(side) with { Structures = list });
        }

        public static RunState WithPerks(this RunState state, Side side, params CardDefinition[] perks)
        {
            List<CardDefinition> list = new List<CardDefinition>(state.SideState(side).Perks);
            list.AddRange(perks);
            bool keystone = state.SideState(side).KeystoneTaken;
            foreach (CardDefinition perk in perks)
            {
                keystone = keystone || perk.IsKeystone;
            }

            return state.WithSideState(state.SideState(side) with { Perks = list, KeystoneTaken = keystone });
        }

        public static StructureInstance Structure(
            string id,
            Side owner,
            ResourceType yieldType,
            int yieldAmount,
            LaneId lane,
            int tier = 1,
            int builtOnTurn = 1,
            Epoch.Core.Effects.ActiveEffect[]? effects = null) =>
            new StructureInstance(
                new StableId(id),
                owner,
                new StableId("TEST_BUILD"),
                tier,
                yieldType,
                yieldAmount,
                lane,
                builtOnTurn,
                effects ?? System.Array.Empty<Epoch.Core.Effects.ActiveEffect>());

        /// <summary>Find a unit by id across both sides, or null if it was destroyed.</summary>
        public static UnitInstance? FindUnit(this RunState state, string id)
        {
            foreach (UnitInstance unit in state.Player.Units)
            {
                if (unit.InstanceId.Value == id)
                {
                    return unit;
                }
            }

            foreach (UnitInstance unit in state.Snapshot.Units)
            {
                if (unit.InstanceId.Value == id)
                {
                    return unit;
                }
            }

            return null;
        }
    }
}
