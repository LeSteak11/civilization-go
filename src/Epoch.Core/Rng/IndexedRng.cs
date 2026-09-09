using Epoch.Core.Domain;

namespace Epoch.Core.Rng
{
    /// <summary>
    /// Indexed stream derivation (Core Spec sec.10.2, [Lock 19], [Final Lock 5]).
    ///
    /// Every authoritative draw is addressed rather than sequential. That is what makes
    /// a collision redraw in one offer slot unable to disturb another turn or slot, and
    /// what keeps presentation or logging from ever shifting the match.
    /// </summary>
    public static class IndexedRng
    {
        /// <summary>ASCII "CARD" — the offer stream's PCG sequence selector.</summary>
        public const ulong CardDomain = 0x43415244UL;

        /// <summary>ASCII "LANE".</summary>
        public const ulong LaneDomain = 0x4C414E45UL;

        /// <summary>ASCII "TIEB".</summary>
        public const ulong TieBreakDomain = 0x54494542UL;

        private const ulong RedrawMultiplier = 0xD1342543DE82EF95UL;

        public static SeedState DeriveStreams(SeedCode masterSeed, string algorithm)
        {
            ulong master = SeedCodec.Decode(masterSeed);
            return DeriveStreams(masterSeed, master, algorithm);
        }

        public static SeedState DeriveStreams(SeedCode masterSeed, ulong masterSeedInt, string algorithm) =>
            new SeedState(
                masterSeed,
                SplitMix64.Next(masterSeedInt ^ 0x01UL),
                SplitMix64.Next(masterSeedInt ^ 0x02UL),
                SplitMix64.Next(masterSeedInt ^ 0x03UL),
                algorithm);

        /// <summary>
        /// The address for one offer slot. <paramref name="redrawAttempt"/> starts at 0
        /// and increments only for the slot that collided (Core Spec sec.6.0).
        /// </summary>
        public static ulong OfferAddress(ulong streamCardOffer, int turn, int offerIndex, int redrawAttempt)
        {
            unchecked
            {
                ulong mixed = streamCardOffer
                    ^ ((ulong)(uint)turn << 32)
                    ^ ((ulong)(uint)offerIndex << 24)
                    ^ ((ulong)(uint)redrawAttempt * RedrawMultiplier);
                return SplitMix64.Next(mixed);
            }
        }

        public static Pcg32 ForOffer(ulong streamCardOffer, int turn, int offerIndex, int redrawAttempt) =>
            new Pcg32(OfferAddress(streamCardOffer, turn, offerIndex, redrawAttempt), CardDomain);

        public static Pcg32 ForLaneModifiers(ulong streamLaneMod) =>
            new Pcg32(streamLaneMod, LaneDomain);

        public static Pcg32 ForTieBreak(ulong streamTieBreak) =>
            new Pcg32(streamTieBreak, TieBreakDomain);
    }
}
