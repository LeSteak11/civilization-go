using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Systems;

namespace Epoch.Application.Runs
{
    /// <summary>
    /// The scripted command source M1 needs: always take the lowest legal offer index,
    /// and for a lane-targeted card the lowest legal lane.
    ///
    /// This is deliberately **not** a strategy. It reads no future offers, consumes no
    /// RNG - authoritative or otherwise - and encodes no balance opinion, so a match it
    /// drives is a pure function of seed and content. Real bot policies are an M3
    /// decision (Implementation Lock 6) and belong nowhere near the Core.
    ///
    /// When nothing is legal it returns a PASS, which is the only way a PASS may ever
    /// arise: voluntary passing is not permitted (Core Spec sec.6.6 [Lock 12]).
    /// </summary>
    public sealed class FirstLegalSelectionSource : SelectionSource
    {
        public Selection Select(RunState state, Side side, CardOfferSet offers, ValidatedContentSet content)
        {
            for (int index = 0; index < offers.Count; index++)
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
                    return new Selection(index, lanes[0]);
                }
            }

            return Selection.Pass;
        }
    }
}
