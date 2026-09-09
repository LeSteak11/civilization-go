using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;

namespace Epoch.Core.Systems
{
    /// <summary>
    /// The common validation pipeline, Core Spec sec.6.1 gates G1-G5, plus the
    /// type-specific rules of sec.6.2-sec.6.5.
    ///
    /// Every gate is evaluated **per side** against the shared hand. Failing any gate
    /// rejects the selection without mutating state (Invariant SM-2).
    /// </summary>
    public static class LegalitySystem
    {
        /// <summary>
        /// Is this offer legal for this side, in this lane? A null lane asks "is it legal
        /// in any lane", which is the question the PASS rule needs answered.
        /// </summary>
        public static ValidationError Check(
            RunState state,
            Side side,
            CardOfferSet offers,
            int offerIndex,
            LaneId? targetLane,
            ValidatedContentSet content)
        {
            // G1 - index in range.
            if (offerIndex < 0 || offerIndex >= offers.Count)
            {
                return ValidationError.ERR_INVALID_INDEX;
            }

            CardDefinition card = content.ById(offers[offerIndex].CardId);
            PlayerState player = state.SideState(side);
            AgeRow age = AgeTable.ForAge(state.Age);

            // G2 - Age eligibility. Keystones inherit minAge 3 from content (K1).
            if (!card.IsEligibleInAge(state.Age))
            {
                return ValidationError.ERR_AGE_LOCKED;
            }

            // G3 - affordability, in the card's own resource.
            int cost = age.CostFor(card.CardType);
            if (player.Resource(card.CostResource) < cost)
            {
                return ValidationError.ERR_UNAFFORDABLE;
            }

            // G5 - target present iff required (B5, T3, A3).
            bool requiresLane = card.CardType == CardType.BUILD || card.CardType == CardType.TRAIN;
            if (requiresLane)
            {
                if (targetLane is null)
                {
                    return ValidationError.ERR_BAD_TARGET;
                }
            }
            else if (targetLane is not null)
            {
                return ValidationError.ERR_BAD_TARGET;
            }

            // G4 - type-specific legality.
            switch (card.CardType)
            {
                case CardType.BUILD:
                    // B4: the payback inequality is NOT a runtime gate in V1 (sec.6.7 X2).
                    // A dominated late BUILD is legal by design; only the offer window closes.
                    return ValidationError.NONE;

                case CardType.TRAIN:
                    // T5: illegal if the lane already holds 3 units of that side.
                    return player.UnitsInLane(targetLane!.Value) >= RunState.MaxUnitsPerLane
                        ? ValidationError.ERR_LANE_FULL
                        : ValidationError.NONE;

                case CardType.ADVANCE:
                    // A5: perks are unique per side per run [Lock 13].
                    if (player.HasPerk(card.CardId))
                    {
                        return ValidationError.ERR_DUPLICATE_PERK;
                    }

                    // K2/K3: maximum one Keystone per side per run.
                    if (card.IsKeystone && player.KeystoneTaken)
                    {
                        return ValidationError.ERR_KEYSTONE_ALREADY_TAKEN;
                    }

                    return ValidationError.NONE;

                default:
                    return ValidationError.ERR_INVALID_INDEX;
            }
        }

        /// <summary>The lanes in which an offer is legal for a side. Empty for ADVANCE and for illegal cards.</summary>
        public static List<LaneId> LegalLanes(
            RunState state,
            Side side,
            CardOfferSet offers,
            int offerIndex,
            ValidatedContentSet content)
        {
            List<LaneId> lanes = new List<LaneId>();
            if (offerIndex < 0 || offerIndex >= offers.Count)
            {
                return lanes;
            }

            CardDefinition card = content.ById(offers[offerIndex].CardId);
            if (card.CardType == CardType.ADVANCE)
            {
                return lanes;
            }

            for (int i = 0; i < state.Lanes.Count; i++)
            {
                LaneId lane = state.Lanes[i].Id;
                if (Check(state, side, offers, offerIndex, lane, content) == ValidationError.NONE)
                {
                    lanes.Add(lane);
                }
            }

            return lanes;
        }

        /// <summary>
        /// Does this side have any legal selection at all? If not, a PASS is forced
        /// (sec.6.6 [Lock 12]). Voluntary passing is never permitted, so this is the only
        /// question that may produce one.
        /// </summary>
        public static bool HasAnyLegalSelection(
            RunState state,
            Side side,
            CardOfferSet offers,
            ValidatedContentSet content)
        {
            for (int i = 0; i < offers.Count; i++)
            {
                CardDefinition card = content.ById(offers[i].CardId);
                if (card.CardType == CardType.ADVANCE)
                {
                    if (Check(state, side, offers, i, null, content) == ValidationError.NONE)
                    {
                        return true;
                    }
                }
                else if (LegalLanes(state, side, offers, i, content).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Validate a submitted selection, including whether a PASS was actually forced.
        /// A PASS that was not forced is <see cref="ValidationError.ERR_PASS_NOT_FORCED"/>:
        /// "If at least one offer is legal for a side, that side must select a legal one."
        /// </summary>
        public static ValidationError Validate(
            RunState state,
            Side side,
            CardOfferSet offers,
            Selection selection,
            ValidatedContentSet content)
        {
            if (selection.IsPass)
            {
                return HasAnyLegalSelection(state, side, offers, content)
                    ? ValidationError.ERR_PASS_NOT_FORCED
                    : ValidationError.NONE;
            }

            return Check(state, side, offers, selection.OfferIndex, selection.TargetLane, content);
        }
    }
}
