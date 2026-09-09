using System;
using System.Collections.Generic;

namespace Epoch.Core.Domain
{
    /// <summary>
    /// Core Spec sec.5.11. One of the three shared offers.
    ///
    /// <c>affordable</c> and <c>legalTargets</c> from the spec are deliberately **not**
    /// stored: they are per side and derived, and storing them on a shared offer is
    /// exactly how the two sides would desynchronise. Legality is computed per side by
    /// <see cref="Systems.LegalitySystem"/> against the same immutable hand.
    /// </summary>
    public sealed record CardOffer(int OfferIndex, StableId CardId);

    /// <summary>
    /// The three shared offers for one turn (Core Spec sec.6.0).
    ///
    /// Invariant CO-1: the three cardIds are identical for PLAYER and SNAPSHOT
    /// (MR sec.1.4 same-seed rule). There is one hand per turn, not one per side.
    /// </summary>
    public sealed record CardOfferSet(int Turn, IReadOnlyList<CardOffer> Offers)
    {
        public CardOffer this[int index] => Offers[index];

        public int Count => Offers.Count;

        public bool ContainsCard(StableId cardId)
        {
            for (int i = 0; i < Offers.Count; i++)
            {
                if (Offers[i].CardId == cardId)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Core Spec sec.5.4 Selection. <see cref="OfferIndex"/> is 0..2 or -1 for a forced
    /// PASS [Lock 12]; TargetLane is required for BUILD and TRAIN and forbidden for
    /// ADVANCE and PASS.
    /// </summary>
    public readonly record struct Selection(int OfferIndex, LaneId? TargetLane)
    {
        public const int PassOfferIndex = -1;

        public static Selection Pass => new Selection(PassOfferIndex, null);

        public bool IsPass => OfferIndex == PassOfferIndex;
    }

    /// <summary>Core Spec sec.5.17.</summary>
    public sealed record MatchResult(
        string RulesVersion,
        string ContentVersion,
        SeedCode Seed,
        int PlayerScore,
        int SnapshotScore,
        int FinalTurn)
    {
        public int ScoreDifferential => PlayerScore - SnapshotScore;

        /// <summary>MC2, MC3: &gt;0 VICTORY, &lt;0 DEFEAT, ==0 TIE. There is no tiebreaker [Lock 4].</summary>
        public MatchOutcome Outcome =>
            ScoreDifferential > 0 ? MatchOutcome.VICTORY
            : ScoreDifferential < 0 ? MatchOutcome.DEFEAT
            : MatchOutcome.TIE;
    }
}
