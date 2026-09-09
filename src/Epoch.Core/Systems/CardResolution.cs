using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Numerics;

namespace Epoch.Core.Systems
{
    /// <summary>
    /// Cost payment (step 5) and card effect application (step 6) of the canonical turn,
    /// Core Spec sec.4.1.
    ///
    /// Both sides' effects are applied to the same pre-effect state and committed
    /// together — no side sees the other's effect while resolving its own
    /// (DERIVED from [Lock 6] simultaneity).
    /// </summary>
    public static class CardResolution
    {
        /// <summary>
        /// Pay and apply for one side. Returns the updated side state; the caller commits
        /// both sides together.
        /// </summary>
        public static PlayerState Apply(
            PlayerState side,
            LaneState? targetLane,
            Selection selection,
            CardOfferSet offers,
            ValidatedContentSet content,
            int turn,
            int age,
            ref int unitSequence,
            ref int structureSequence,
            List<AppliedCard> log)
        {
            if (selection.IsPass)
            {
                // sec.6.6: no card effect, no cost paid, no compensation.
                log.Add(AppliedCard.Pass(side.Side));
                return side;
            }

            CardDefinition card = content.ById(offers[selection.OfferIndex].CardId);
            AgeRow ageRow = AgeTable.ForAge(age);
            int cost = ageRow.CostFor(card.CardType);

            // E13: pay. E15: the floor is hard; legality (G3) already guaranteed it.
            PlayerState paid = side.WithResource(
                card.CostResource,
                side.Resource(card.CostResource) - cost);

            switch (card.CardType)
            {
                case CardType.BUILD:
                    return ApplyBuild(paid, card, targetLane!, ageRow, cost, turn, ref structureSequence, log);

                case CardType.TRAIN:
                    return ApplyTrain(paid, card, targetLane!, ageRow, turn, ref unitSequence, log);

                case CardType.ADVANCE:
                    return ApplyAdvance(paid, card, log);

                default:
                    return paid;
            }
        }

        private static PlayerState ApplyBuild(
            PlayerState side,
            CardDefinition card,
            LaneState lane,
            AgeRow age,
            int cost,
            int turn,
            ref int structureSequence,
            List<AppliedCard> log)
        {
            // B2 [Final Lock 3]: tier and base yield equal the resolving Age. No tier choice.
            ResourceType yieldType = card.YieldType ?? ResourceType.GROWTH;
            int yieldAmount = age.TierYield;

            // B3: RIVER adds +1, but only to a Growth yield (MR sec.1.3).
            if (lane.Modifier == LaneModifier.RIVER && yieldType == ResourceType.GROWTH)
            {
                yieldAmount += 1;
            }

            structureSequence++;
            StructureInstance structure = new StructureInstance(
                new StableId("S" + FixedValue.IntToString(structureSequence)),
                side.Side,
                card.CardId,
                age.StructureTier,
                yieldType,
                yieldAmount,
                lane.Id,
                turn,
                // A BUILD's own effects travel with the instance, so a structure that
                // grants Power in its lane keeps granting it for the rest of the run.
                card.Effects)
            {
                // X5: telemetry only, never a legality gate.
                PaybackTurnsHundredths = yieldAmount == 0 ? 0 : (cost * FixedValue.Scale) / yieldAmount,
            };

            List<StructureInstance> structures = new List<StructureInstance>(side.Structures) { structure };
            log.Add(AppliedCard.Build(side.Side, card.CardId, lane.Id, yieldType, yieldAmount));
            return side with { Structures = structures };
        }

        private static PlayerState ApplyTrain(
            PlayerState side,
            CardDefinition card,
            LaneState lane,
            AgeRow age,
            int turn,
            ref int unitSequence,
            List<AppliedCard> log)
        {
            unitSequence++;

            // T4 [Lock 8]: PLAYER enters at tile 1, SNAPSHOT at tile 5.
            // T2: basePower is the resolving Age's unitPower, fixed forever (Invariant UI-1).
            // T10 [Lock 5]: full HP.
            UnitInstance unit = new UnitInstance(
                new StableId("U" + FixedValue.IntToString(unitSequence)),
                side.Side,
                card.CardId,
                card.UnitClass ?? UnitClass.SWORD,
                age.UnitPower,
                UnitInstance.MaxHp,
                lane.Id,
                side.EntryTile,
                card.GrantsReach,
                turn,
                null);

            List<UnitInstance> units = new List<UnitInstance>(side.Units) { unit };
            log.Add(AppliedCard.Train(side.Side, card.CardId, lane.Id, unit.InstanceId));
            return side with { Units = units };
        }

        private static PlayerState ApplyAdvance(PlayerState side, CardDefinition card, List<AppliedCard> log)
        {
            // A2: grant the perk. A6 [Lock 2]: ADVANCE never accelerates the Age.
            List<CardDefinition> perks = new List<CardDefinition>(side.Perks) { card };
            log.Add(AppliedCard.Advance(side.Side, card.CardId, card.IsKeystone));
            return side with
            {
                Perks = perks,
                KeystoneTaken = side.KeystoneTaken || card.IsKeystone,
            };
        }
    }

    /// <summary>What a side's card actually did this turn, for event emission.</summary>
    public sealed record AppliedCard(
        Side Side,
        CardType? CardType,
        StableId CardId,
        LaneId? Lane,
        bool IsPass,
        bool IsKeystone,
        ResourceType? YieldType,
        int YieldAmount,
        StableId UnitId)
    {
        public static AppliedCard Pass(Side side) =>
            new AppliedCard(side, null, default, null, true, false, null, 0, default);

        public static AppliedCard Build(Side side, StableId cardId, LaneId lane, ResourceType yieldType, int yieldAmount) =>
            new AppliedCard(side, Domain.CardType.BUILD, cardId, lane, false, false, yieldType, yieldAmount, default);

        public static AppliedCard Train(Side side, StableId cardId, LaneId lane, StableId unitId) =>
            new AppliedCard(side, Domain.CardType.TRAIN, cardId, lane, false, false, null, 0, unitId);

        public static AppliedCard Advance(Side side, StableId cardId, bool isKeystone) =>
            new AppliedCard(side, Domain.CardType.ADVANCE, cardId, null, false, isKeystone, null, 0, default);
    }
}
