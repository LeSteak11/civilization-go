using Epoch.Core.Domain;

namespace Epoch.Core.Commands
{
    /// <summary>
    /// The single decision a side makes in a turn (Core Spec sec.5.4 Selection).
    /// OfferIndex is 0..2, or <see cref="PassOfferIndex"/> for a forced PASS.
    /// PASS is never voluntary (Core Spec sec.6.6 [Lock 12]).
    /// TargetLane is required for BUILD and TRAIN, forbidden for ADVANCE and PASS.
    /// </summary>
    public sealed record CardSelectionCommand(
        int Turn,
        Side Side,
        int OfferIndex,
        LaneId? TargetLane) : TurnCommand(Turn, Side)
    {
        public const int PassOfferIndex = -1;

        public bool IsPass => OfferIndex == PassOfferIndex;
    }
}
