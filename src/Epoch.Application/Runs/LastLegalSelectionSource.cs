using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Systems;

namespace Epoch.Application.Runs
{
    /// <summary>
    /// The mirror of <see cref="FirstLegalSelectionSource"/>: take the highest legal
    /// offer index, and the highest legal lane.
    ///
    /// Its only purpose is asymmetry. Two sides running the identical policy mirror each
    /// other exactly - they meet on tile 3 on the same turn with the same Power, so every
    /// contested tile is disputed and every match ends 0-0. That is the correct outcome
    /// for identical play, but it means a symmetric fixture never exercises scoring,
    /// ownership persistence, contested income, or the HIGHLAND bonus. Pairing this
    /// against the first-legal policy makes a full run actually reach those rules.
    ///
    /// Like its counterpart this is not a strategy: it reads no future offers and
    /// consumes no RNG, so a match remains a pure function of seed and content.
    /// </summary>
    public sealed class LastLegalSelectionSource : SelectionSource
    {
        public Selection Select(RunState state, Side side, CardOfferSet offers, ValidatedContentSet content)
        {
            for (int index = offers.Count - 1; index >= 0; index--)
            {
                CardDefinition card = content.ById(offers[index].CardId);

                if (card.CardType == CardType.ADVANCE)
                {
                    if (LegalitySystem.Check(state, side, offers, index, null, content) == ValidationError.NONE)
                    {
                        return new Selection(index, null);
                    }

                    continue;
                }

                List<LaneId> lanes = LegalitySystem.LegalLanes(state, side, offers, index, content);
                if (lanes.Count > 0)
                {
                    return new Selection(index, lanes[lanes.Count - 1]);
                }
            }

            return Selection.Pass;
        }
    }
}
