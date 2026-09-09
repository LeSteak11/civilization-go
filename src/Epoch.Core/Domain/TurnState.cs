namespace Epoch.Core.Domain
{
    /// <summary>
    /// Core Spec sec.5.4. The shared, immutable-for-the-turn offer set plus each side's
    /// selection. Offers are identical for both sides (Invariant CO-1); legality is
    /// evaluated per side afterwards and never mutates the shared hand.
    /// Offer contents are defined in M1 with the offer generator.
    /// </summary>
    public sealed record TurnState(
        int Turn,
        int Age,
        CardOfferSet Offers,
        Commands.CardSelectionCommand? PlayerSelection,
        Commands.CardSelectionCommand? SnapshotSelection);

    /// <summary>The three shared offers for one turn (Core Spec sec.5.11, sec.6.0). Populated in M1.</summary>
    public sealed record CardOfferSet(int Turn);
}
