using System;
using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Rng;

namespace Epoch.Core.Systems
{
    /// <summary>
    /// Shared offer generation, Core Spec sec.6.0 and [Final Lock 2].
    ///
    /// Three independent weighted CardType draws at indexed RNG addresses. Duplicate
    /// **types** are permitted; duplicate **cardIds** are not, and a collision redraws
    /// that slot from the same indexed stream with an incremented redrawAttempt so the
    /// retry cannot disturb another slot or turn.
    ///
    /// The hand is shared and immutable for the turn. Owned perks, resources, lane
    /// capacity and prior choices never affect generation — PLAYER and SNAPSHOT always
    /// see identical offers (Invariant CO-1, MR sec.1.4). Legality is evaluated per side
    /// afterwards, and an illegal card stays visible but unselectable.
    /// </summary>
    public static class OfferGeneration
    {
        /// <summary>The weighted type draw is over percentage points that sum to 100.</summary>
        private const uint WeightTotal = 100;

        /// <summary>
        /// A hand cannot need more redraws than there are distinct cards; this bound only
        /// exists so that a malformed content set fails loudly instead of hanging.
        /// </summary>
        private const int MaxRedrawAttempts = 4096;

        public static CardOfferSet Generate(SeedState seed, int turn, ValidatedContentSet content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            int age = RunState.AgeForTurn(turn);
            OfferWeightBand weights = OfferWeights.ForTurn(turn);
            List<CardOffer> hand = new List<CardOffer>(RunState.CardsOfferedPerTurn);

            for (int slot = 0; slot < RunState.CardsOfferedPerTurn; slot++)
            {
                bool placed = false;
                for (int redraw = 0; redraw < MaxRedrawAttempts; redraw++)
                {
                    Pcg32 rng = IndexedRng.ForOffer(seed.StreamCardOffer, turn, slot, redraw);

                    // Each address draws card type first, then the card index (sec.10.2).
                    uint roll = rng.NextBounded(WeightTotal);
                    CardType type = TypeForRoll(roll, weights);

                    List<CardDefinition> pool = content.EligiblePool(type, age);
                    if (pool.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Content validation must guarantee a non-empty pool wherever a type has non-zero weight (Core Spec sec.6.0): " +
                            type + " at Age " + age + ".");
                    }

                    CardDefinition card = pool[(int)rng.NextBounded((uint)pool.Count)];

                    if (!ContainsCard(hand, card.CardId))
                    {
                        hand.Add(new CardOffer(slot, card.CardId));
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    throw new InvalidOperationException(
                        "Offer slot " + slot + " on turn " + turn + " could not be filled with a distinct cardId.");
                }
            }

            return new CardOfferSet(turn, hand);
        }

        /// <summary>
        /// Cumulative selection in the weight table's own column order: BUILD, then TRAIN,
        /// then ADVANCE. A zero weight is unreachable because the comparison is strict,
        /// which is what closes the BUILD window on turns 21-24 (V1 Build Economy Exception X4).
        /// </summary>
        internal static CardType TypeForRoll(uint roll, OfferWeightBand weights)
        {
            if (roll < (uint)weights.Build)
            {
                return CardType.BUILD;
            }

            return roll < (uint)(weights.Build + weights.Train) ? CardType.TRAIN : CardType.ADVANCE;
        }

        private static bool ContainsCard(List<CardOffer> hand, StableId cardId)
        {
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].CardId == cardId)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Lane modifier assignment, [Lock 10].
    ///
    /// Every run contains exactly one RIVER, one HIGHLAND and one COAST, shuffled by the
    /// dedicated <c>streamLaneMod</c> stream. Both sides play the same layout — there is
    /// one board. Exactly 3! = 6 layouts are possible, uniformly distributed.
    /// </summary>
    public static class LaneModifierAssignment
    {
        public static LaneState[] Assign(SeedState seed)
        {
            LaneModifier[] modifiers =
            {
                LaneModifier.RIVER,
                LaneModifier.HIGHLAND,
                LaneModifier.COAST,
            };

            Pcg32 rng = IndexedRng.ForLaneModifiers(seed.StreamLaneMod);

            // Fisher-Yates, descending, with an unbiased bounded draw at each step.
            for (int i = modifiers.Length - 1; i > 0; i--)
            {
                int j = (int)rng.NextBounded((uint)(i + 1));
                LaneModifier swap = modifiers[i];
                modifiers[i] = modifiers[j];
                modifiers[j] = swap;
            }

            return new[]
            {
                LaneState.New(LaneId.A, modifiers[0]),
                LaneState.New(LaneId.B, modifiers[1]),
                LaneState.New(LaneId.C, modifiers[2]),
            };
        }
    }
}
