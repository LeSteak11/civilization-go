namespace Epoch.Core.Domain
{
    /// <summary>
    /// Core Spec sec.5.4. The shared, immutable-for-the-turn offer set plus each side's
    /// selection.
    ///
    /// Offers are identical for both sides (Invariant CO-1); legality is evaluated per
    /// side afterwards and never mutates the shared hand. The offer set itself lives in
    /// CardOffer.cs, because it outlives any one turn's selections.
    /// </summary>
    public sealed record TurnState(
        int Turn,
        int Age,
        CardOfferSet Offers,
        Selection? PlayerSelection,
        Selection? SnapshotSelection);
}
