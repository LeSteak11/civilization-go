namespace Epoch.Core.Rng
{
    /// <summary>
    /// PCG32 XSH-RR 64/32 — the sole authoritative generator, pinned by
    /// rulesVersion 1.2.0-v1-final (Core Spec sec.10.2, [Final Lock 5]).
    ///
    /// Language- and engine-native RNG is forbidden in authoritative simulation
    /// [Lock 19]; the architecture guard bans System.Random from the Core outright.
    ///
    /// A mutable struct is deliberate: each indexed address gets its own short-lived
    /// generator, so there is no shared stream to desynchronise. Copy semantics mean an
    /// accidental copy advances independently rather than silently sharing state.
    /// </summary>
    public struct Pcg32 : DeterministicRng
    {
        private const ulong Multiplier = 6364136223846793005UL;

        private ulong _state;
        private ulong _inc;

        /// <summary>
        /// The reference two-step initialization the spec names verbatim:
        /// state = 0; inc = (initseq &lt;&lt; 1) | 1; next(); state += initstate; next().
        /// </summary>
        public Pcg32(ulong initState, ulong initSeq)
        {
            unchecked
            {
                _state = 0UL;
                _inc = (initSeq << 1) | 1UL;
                NextUInt32();
                _state += initState;
                NextUInt32();
            }
        }

        public uint NextUInt32()
        {
            unchecked
            {
                ulong oldState = _state;
                _state = (oldState * Multiplier) + _inc;
                uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
                int rot = (int)(oldState >> 59);
                return RotateRight32(xorShifted, rot);
            }
        }

        /// <summary>
        /// Unbiased bounded draw by rejection sampling (Core Spec sec.10.2).
        /// "No modulo-only bounded selection is permitted" — the threshold discards the
        ///low tail that would otherwise skew small bounds.
        /// </summary>
        public uint NextBounded(uint exclusiveUpperBound)
        {
            if (exclusiveUpperBound == 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(exclusiveUpperBound), "Bound must be at least 1.");
            }

            unchecked
            {
                // threshold = uint32(-bound) mod bound  ==  (2^32 - bound) mod bound
                uint threshold = (uint)((0UL - exclusiveUpperBound) % exclusiveUpperBound);
                while (true)
                {
                    uint r = NextUInt32();
                    if (r >= threshold)
                    {
                        return r % exclusiveUpperBound;
                    }
                }
            }
        }

        private static uint RotateRight32(uint value, int rot)
        {
            unchecked
            {
                return (value >> rot) | (value << ((-rot) & 31));
            }
        }
    }
}
