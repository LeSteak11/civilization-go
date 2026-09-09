using System;
using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Events;
using Epoch.Core.Numerics;
using Epoch.Core.Rng;
using Epoch.Core.Serialization;
using Epoch.Core.Systems;

namespace Epoch.Core.StateMachine
{
    /// <summary>
    /// The canonical turn pipeline, Core Spec sec.4.1 steps 1-16.
    ///
    /// MR sec.1.2 fixes the spine in one sentence - "yields tick, lanes push forward one
    /// tile, contested tiles fight" - which locks card, then income, then movement, then
    /// combat. Everything else here is the locked expansion of that order.
    ///
    /// Resolution completes **immediately** and returns an ordered event list; animation
    /// is a later, non-authoritative consumer (Technical Plan sec.8). Nothing in this
    /// class touches a clock, a filesystem, a logger or a native RNG.
    /// </summary>
    public static class MatchResolver
    {
        /// <summary>
        /// Step 2: generate the shared hand. Pure - it mutates nothing and is identical
        /// for both sides (Invariant CO-1).
        /// </summary>
        public static CardOfferSet GenerateOffers(RunState state, ValidatedContentSet content) =>
            OfferGeneration.Generate(state.Seed, state.Turn, content);

        /// <summary>
        /// Steps 4-16. <paramref name="playerSelection"/> and
        /// <paramref name="snapshotSelection"/> are validated first; an invalid selection
        /// returns a failed <see cref="ResolvedTurn"/> having mutated nothing (SM-2).
        /// </summary>
        public static ResolvedTurn Resolve(
            RunState state,
            CardOfferSet offers,
            Selection playerSelection,
            Selection snapshotSelection,
            ValidatedContentSet content,
            int startingSequence)
        {
            if (state.Result is not null)
            {
                throw new InvalidOperationException("The run has already completed (Core Spec sec.3.2 S30).");
            }

            // Step 4: validate BOTH selections against the immutable pre-turn state.
            ValidationError playerError =
                LegalitySystem.Validate(state, Side.PLAYER, offers, playerSelection, content);
            if (playerError != ValidationError.NONE)
            {
                return ResolvedTurn.Rejected(Side.PLAYER, playerError);
            }

            ValidationError snapshotError =
                LegalitySystem.Validate(state, Side.SNAPSHOT, offers, snapshotSelection, content);
            if (snapshotError != ValidationError.NONE)
            {
                return ResolvedTurn.Rejected(Side.SNAPSHOT, snapshotError);
            }

            EventLog log = new EventLog(startingSequence);
            int turn = state.Turn;

            log.Add(turn, RunPhase.CARD_GENERATION, null, SimulationEventType.OFFER_GENERATED,
                OfferPayload(offers));

            RunState working = state with { Phase = RunPhase.ACTION_VALIDATION };

            // Steps 5-6: pay and apply. Both sides read the SAME pre-effect state and are
            // committed together, so neither sees the other's effect while resolving its
            // own (DERIVED from [Lock 6]).
            int unitSequence = working.UnitSequence;
            int structureSequence = working.StructureSequence;
            List<AppliedCard> applied = new List<AppliedCard>(2);

            PlayerState playerAfterCard = CardResolution.Apply(
                working.Player,
                playerSelection.TargetLane is null ? null : working.Lane(playerSelection.TargetLane.Value),
                playerSelection,
                offers,
                content,
                turn,
                working.Age,
                ref unitSequence,
                ref structureSequence,
                applied);

            PlayerState snapshotAfterCard = CardResolution.Apply(
                working.Snapshot,
                snapshotSelection.TargetLane is null ? null : working.Lane(snapshotSelection.TargetLane.Value),
                snapshotSelection,
                offers,
                content,
                turn,
                working.Age,
                ref unitSequence,
                ref structureSequence,
                applied);

            working = working with
            {
                Player = playerAfterCard,
                Snapshot = snapshotAfterCard,
                UnitSequence = unitSequence,
                StructureSequence = structureSequence,
                Phase = RunPhase.CARD_EFFECT_APPLY,
            };

            EmitCardEvents(log, turn, applied, offers, playerSelection, snapshotSelection, content, state);

            // Step 7: income. Reads tile3HolderPrevTurn, never this turn's ownership.
            working = IncomeSystem.ApplyBoth(
                working,
                out IncomeBreakdown playerGrowth,
                out IncomeBreakdown playerInsight,
                out IncomeBreakdown snapshotGrowth,
                out IncomeBreakdown snapshotInsight) with { Phase = RunPhase.INCOME };

            log.Add(turn, RunPhase.INCOME, Side.PLAYER, SimulationEventType.INCOME_GRANTED,
                IncomePayload(playerGrowth, playerInsight));
            log.Add(turn, RunPhase.INCOME, Side.SNAPSHOT, SimulationEventType.INCOME_GRANTED,
                IncomePayload(snapshotGrowth, snapshotInsight));

            // Step 8: simultaneous step-wise movement. No combat between steps (M13).
            List<UnitMove> moves = new List<UnitMove>();
            working = MovementSystem.Resolve(working, moves) with { Phase = RunPhase.MOVEMENT };
            for (int i = 0; i < moves.Count; i++)
            {
                UnitMove move = moves[i];
                log.Add(turn, RunPhase.MOVEMENT, move.Owner, SimulationEventType.UNIT_MOVED,
                    new PayloadBuilder()
                        .Add("unit", move.UnitId.Value)
                        .Add("lane", move.Lane.ToString())
                        .Add("from", move.FromTile)
                        .Add("to", move.ToTile)
                        .ToString());
            }

            // Steps 10-11: combat at every co-occupied tile, damage applied simultaneously.
            List<CombatOutcome> combats = new List<CombatOutcome>();
            List<StableId> destroyed = new List<StableId>();
            working = CombatSystem.Resolve(working, combats, destroyed) with { Phase = RunPhase.COMBAT };

            for (int i = 0; i < combats.Count; i++)
            {
                log.Add(turn, RunPhase.COMBAT, null, SimulationEventType.COMBAT_RESOLVED, CombatPayload(combats[i]));
            }

            for (int i = 0; i < destroyed.Count; i++)
            {
                log.Add(turn, RunPhase.COMBAT, null, SimulationEventType.UNIT_DESTROYED,
                    new PayloadBuilder().Add("unit", destroyed[i].Value).ToString());
            }

            // Steps 12-14: ownership computed once, score awarded, ownership persisted.
            int playerScoreBefore = working.Player.Score;
            int snapshotScoreBefore = working.Snapshot.Score;
            List<OwnershipChange> ownership = new List<OwnershipChange>();
            working = ScoringSystem.Resolve(working, ownership) with { Phase = RunPhase.SCORE_UPDATE };

            for (int i = 0; i < ownership.Count; i++)
            {
                OwnershipChange change = ownership[i];
                log.Add(turn, RunPhase.SCORE_UPDATE, change.To, SimulationEventType.OWNERSHIP_CHANGED,
                    new PayloadBuilder()
                        .Add("lane", change.Lane.ToString())
                        .Add("from", change.From is null ? "-" : change.From.Value.ToString())
                        .Add("to", change.To is null ? "-" : change.To.Value.ToString())
                        .ToString());
            }

            if (working.Player.Score != playerScoreBefore)
            {
                log.Add(turn, RunPhase.SCORE_UPDATE, Side.PLAYER, SimulationEventType.SCORE_CHANGED,
                    new PayloadBuilder()
                        .Add("delta", working.Player.Score - playerScoreBefore)
                        .Add("total", working.Player.Score)
                        .ToString());
            }

            if (working.Snapshot.Score != snapshotScoreBefore)
            {
                log.Add(turn, RunPhase.SCORE_UPDATE, Side.SNAPSHOT, SimulationEventType.SCORE_CHANGED,
                    new PayloadBuilder()
                        .Add("delta", working.Snapshot.Score - snapshotScoreBefore)
                        .Add("total", working.Snapshot.Score)
                        .ToString());
            }

            // Step 15: Age transition, effective from the NEXT turn [Lock 1], [Lock 2].
            // Turn 6 resolves entirely under Age I; turn 7 is the first Age II turn.
            working = working with { Phase = RunPhase.AGE_TRANSITION_CHECK };
            bool ageAdvances = turn % RunState.TurnsPerAge == 0 && turn < RunState.TurnsPerRun;
            if (ageAdvances)
            {
                log.Add(turn, RunPhase.AGE_TRANSITION_CHECK, null, SimulationEventType.AGE_ADVANCED,
                    new PayloadBuilder().Add("age", working.Age + 1).ToString());
            }

            // Step 16: turn complete.
            if (turn < RunState.TurnsPerRun)
            {
                working = working with
                {
                    Turn = turn + 1,
                    Age = RunState.AgeForTurn(turn + 1),
                    Phase = RunPhase.TURN_COMPLETE,
                };
            }
            else
            {
                // MC1: the result is the score differential at turn 24, never a board wipe.
                working = working with
                {
                    Phase = RunPhase.MATCH_COMPLETE,
                    Result = new MatchResult(
                        working.RulesVersion,
                        working.ContentVersion,
                        working.Seed.MasterSeed,
                        working.Player.Score,
                        working.Snapshot.Score,
                        RunState.TurnsPerRun),
                };
            }

            log.Add(turn, RunPhase.TURN_COMPLETE, null, SimulationEventType.TURN_COMPLETED,
                new PayloadBuilder()
                    .Add("playerScore", working.Player.Score)
                    .Add("snapshotScore", working.Snapshot.Score)
                    .ToString());

            return ResolvedTurn.Accepted(working, log.Events, CanonicalState.Write(working), log.NextSequence);
        }

        private static void EmitCardEvents(
            EventLog log,
            int turn,
            List<AppliedCard> applied,
            CardOfferSet offers,
            Selection playerSelection,
            Selection snapshotSelection,
            ValidatedContentSet content,
            RunState preState)
        {
            for (int i = 0; i < applied.Count; i++)
            {
                AppliedCard card = applied[i];
                Selection selection = card.Side == Side.PLAYER ? playerSelection : snapshotSelection;

                if (card.IsPass)
                {
                    // sec.6.6 item 5: a PASS is recorded as offerIndex -1 with no target.
                    log.Add(turn, RunPhase.CHOICE_WINDOW, card.Side, SimulationEventType.CARD_SELECTED,
                        new PayloadBuilder().Add("offerIndex", Selection.PassOfferIndex).Add("pass", true).ToString());
                    continue;
                }

                CardDefinition definition = content.ById(offers[selection.OfferIndex].CardId);
                int cost = AgeTable.ForAge(preState.Age).CostFor(definition.CardType);

                log.Add(turn, RunPhase.CHOICE_WINDOW, card.Side, SimulationEventType.CARD_SELECTED,
                    new PayloadBuilder()
                        .Add("offerIndex", selection.OfferIndex)
                        .Add("card", definition.CardId.Value)
                        .Add("lane", selection.TargetLane is null ? "-" : selection.TargetLane.Value.ToString())
                        .ToString());

                log.Add(turn, RunPhase.COST_PAYMENT, card.Side, SimulationEventType.COST_PAID,
                    new PayloadBuilder()
                        .Add("resource", definition.CostResource.ToString())
                        .Add("cost", cost)
                        .ToString());

                PayloadBuilder effect = new PayloadBuilder()
                    .Add("card", definition.CardId.Value)
                    .Add("type", definition.CardType.ToString());
                if (card.Lane is not null)
                {
                    effect.Add("lane", card.Lane.Value.ToString());
                }

                if (card.YieldType is not null)
                {
                    effect.Add("yieldType", card.YieldType.Value.ToString()).Add("yield", card.YieldAmount);
                }

                if (!card.UnitId.IsEmpty)
                {
                    effect.Add("unit", card.UnitId.Value);
                }

                if (card.IsKeystone)
                {
                    effect.Add("keystone", true);
                }

                log.Add(turn, RunPhase.CARD_EFFECT_APPLY, card.Side, SimulationEventType.EFFECT_APPLIED, effect.ToString());
            }
        }

        private static string OfferPayload(CardOfferSet offers)
        {
            PayloadBuilder payload = new PayloadBuilder();
            for (int i = 0; i < offers.Count; i++)
            {
                payload.Add("slot" + FixedValue.IntToString(i), offers[i].CardId.Value);
            }

            return payload.ToString();
        }

        private static string IncomePayload(IncomeBreakdown growth, IncomeBreakdown insight) =>
            new PayloadBuilder()
                .Add("growth", growth.Total)
                .Add("growthBase", growth.Base)
                .Add("growthStructures", growth.Structures)
                .Add("growthContested", growth.Contested)
                .Add("insight", insight.Total)
                .Add("insightBase", insight.Base)
                .Add("insightStructures", insight.Structures)
                .Add("insightContested", insight.Contested)
                .ToString();

        private static string CombatPayload(CombatOutcome combat) =>
            new PayloadBuilder()
                .Add("lane", combat.Lane.ToString())
                .Add("tile", combat.Tile)
                .Add("playerPower", combat.PlayerPower.ToString())
                .Add("snapshotPower", combat.SnapshotPower.ToString())
                .Add("delta", combat.PlayerDelta)
                .Add("playerDamage", combat.PlayerDamageDealt)
                .Add("snapshotDamage", combat.SnapshotDamageDealt)
                .Add("playerCounter", combat.PlayerCountered)
                .Add("snapshotCounter", combat.SnapshotCountered)
                .ToString();
    }

    /// <summary>
    /// The outcome of asking the Core to resolve a turn: either a rejection that mutated
    /// nothing, or the next authoritative state with its ordered events and canonical bytes.
    /// </summary>
    public sealed record ResolvedTurn(
        bool IsAccepted,
        RunState? NextState,
        IReadOnlyList<SimulationEvent> Events,
        string CanonicalState,
        int NextSequence,
        Side? RejectedSide,
        ValidationError Error)
    {
        public static ResolvedTurn Accepted(
            RunState next,
            IReadOnlyList<SimulationEvent> events,
            string canonicalState,
            int nextSequence) =>
            new ResolvedTurn(true, next, events, canonicalState, nextSequence, null, ValidationError.NONE);

        public static ResolvedTurn Rejected(Side side, ValidationError error) =>
            new ResolvedTurn(false, null, Array.Empty<SimulationEvent>(), string.Empty, 0, side, error);
    }
}
