namespace Epoch.Core.Rng
{
    /// <summary>
    /// Stafford variant-13 SplitMix64, used ONLY for indexed seed derivation
    /// (Core Spec sec.10.2, LOCKED-V1 [Final Lock 5]). It is not the match generator —
    /// PCG32 is. Every operation is unsigned modulo 2^64 and every shift is logical.
    /// </summary>
    public static class SplitMix64
    {
        public const ulong Gamma = 0x9E3779B97F4A7C15UL;

        private const ulong Multiplier1 = 0xBF58476D1CE4E5B9UL;
        private const ulong Multiplier2 = 0x94D049BB133111EBUL;

        /// <summary>
        /// The spec's <c>splitmix64(x)</c>: advance by gamma, then finalize.
        /// Vector SM-00: splitmix64(0) == E220A8397B1DCDAF.
        /// </summary>
        public static ulong Next(ulong x)
        {
            unchecked
            {
                ulong z = x + Gamma;
                z = (z ^ (z >> 30)) * Multiplier1;
                z = (z ^ (z >> 27)) * Multiplier2;
                return z ^ (z >> 31);
            }
        }
    }
}
