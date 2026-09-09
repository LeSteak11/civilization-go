using Epoch.Core.Domain;

namespace Epoch.Core.Commands
{
    /// <summary>
    /// The single decision a side makes in a turn (Core Spec sec.5.4 Selection,
    /// Technical Plan sec.6).
    ///
    /// A command is what the Application layer submits; <see cref="Domain.Selection"/> is
    /// the same choice as authoritative state. They are kept distinct because a command
    /// carries the turn and side it claims to be for, which is exactly what the
    /// coordinator must check before trusting it.
    ///
    /// OfferIndex is 0..2, or <see cref="PassOfferIndex"/> for a forced PASS. PASS is
    /// never voluntary (Core Spec sec.6.6 [Lock 12]). TargetLane is required for BUILD and
    /// TRAIN and forbidden for ADVANCE and PASS.
    /// </summary>
    public sealed record CardSelectionCommand(
        int Turn,
        Side Side,
        int OfferIndex,
        LaneId? TargetLane) : TurnCommand(Turn, Side)
    {
        public const int PassOfferIndex = Selection.PassOfferIndex;

        public bool IsPass => OfferIndex == PassOfferIndex;

        public Selection ToSelection() => new Selection(OfferIndex, TargetLane);

        public static CardSelectionCommand From(int turn, Side side, Selection selection) =>
            new CardSelectionCommand(turn, side, selection.OfferIndex, selection.TargetLane);

        public static CardSelectionCommand Pass(int turn, Side side) =>
            new CardSelectionCommand(turn, side, PassOfferIndex, null);
    }
}
