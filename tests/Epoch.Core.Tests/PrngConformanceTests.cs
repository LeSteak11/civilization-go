using System;
using Epoch.Core.Domain;
using Epoch.Core.Rng;
using Epoch.Testing;

namespace Epoch.Core.Tests
{
    /// <summary>
    /// GT-30 PRNG portability. Every vector here is transcribed from Core Spec sec.10.2,
    /// which calls them normative: they "must pass unchanged on every supported platform".
    ///
    /// These are the tests that make the seed a contract rather than an implementation
    /// detail. If one fails, every offer sequence in every recorded match is invalid.
    /// </summary>
    public static class PrngConformanceTests
    {
        [TestCase("RNG-01", "SplitMix64 reproduces vectors SM-00, SM-01 and SM-02")]
        public static void SplitMix64Vectors()
        {
            Assert.Equal(0xE220A8397B1DCDAFUL, SplitMix64.Next(0UL), "SM-00");
            Assert.Equal(0x910A2DEC89025CC1UL, SplitMix64.Next(1UL), "SM-01");
            Assert.Equal(0x157A3807A48FAA9DUL, SplitMix64.Next(0x0123456789ABCDEFUL), "SM-02");
        }

        [TestCase("RNG-02", "PCG32 reproduces vector PCG-00: initstate 42, initseq 54, first six outputs")]
        public static void Pcg32ReferenceStream()
        {
            uint[] expected =
            {
                2707161783, 2068313097, 3122475824, 2211639955, 3215226955, 3421331566,
            };

            Pcg32 rng = new Pcg32(42UL, 54UL);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], rng.NextUInt32(), "PCG-00 output " + i);
            }
        }

        [TestCase("RNG-03", "Stream derivation reproduces vectors STR-00 and STR-01")]
        public static void StreamDerivation()
        {
            SeedState zero = IndexedRng.DeriveStreams(new SeedCode("0"), 0UL, SeedState.PinnedAlgorithm);
            Assert.Equal(0x910A2DEC89025CC1UL, zero.StreamCardOffer, "STR-00 card");
            Assert.Equal(0x975835DE1C9756CEUL, zero.StreamLaneMod, "STR-00 lane");
            Assert.Equal(0x1D0B14E4DB018FEDUL, zero.StreamTieBreak, "STR-00 tie");

            SeedState mixed = IndexedRng.DeriveStreams(
                new SeedCode("0"), 0x0123456789ABCDEFUL, SeedState.PinnedAlgorithm);
            Assert.Equal(0xE821EEBBC0778421UL, mixed.StreamCardOffer, "STR-01 card");
            Assert.Equal(0x629DD08FAE280E80UL, mixed.StreamLaneMod, "STR-01 lane");
            Assert.Equal(0x9B4C210F98EC07FAUL, mixed.StreamTieBreak, "STR-01 tie");
        }

        [TestCase("RNG-04", "Indexed offer addresses reproduce vector OFF-19, turn 19")]
        public static void IndexedOfferAddresses()
        {
            // Core Spec sec.10.2 labels OFF-19 "master 1", but its own numbers derive from
            // the card stream 910A2DEC89025CC1, which STR-00 gives for master 0. The two
            // vector rows cross-check each other and the label is the odd one out, so the
            // numbers are taken as normative and the label as a typo. Reported to the owner.
            SeedState seed = IndexedRng.DeriveStreams(new SeedCode("0"), 0UL, SeedState.PinnedAlgorithm);
            Assert.Equal(0x910A2DEC89025CC1UL, seed.StreamCardOffer, "OFF-19 is anchored to the STR-00 card stream");

            ulong[] expected =
            {
                0x933FE6C326E506C1UL, 0x468BE5E4E85FCD29UL, 0xF3385872DB25850CUL,
            };

            for (int slot = 0; slot < expected.Length; slot++)
            {
                Assert.Equal(
                    expected[slot],
                    IndexedRng.OfferAddress(seed.StreamCardOffer, 19, slot, 0),
                    "OFF-19 slot " + slot);
            }
        }

        [TestCase("RNG-05", "PCG at those addresses reproduces vector OFF-19-OUT")]
        public static void IndexedOfferFirstOutputs()
        {
            SeedState seed = IndexedRng.DeriveStreams(new SeedCode("0"), 0UL, SeedState.PinnedAlgorithm);

            uint[] expected = { 4207010209, 4199241477, 3060309534 };

            for (int slot = 0; slot < expected.Length; slot++)
            {
                Pcg32 rng = IndexedRng.ForOffer(seed.StreamCardOffer, 19, slot, 0);
                Assert.Equal(expected[slot], rng.NextUInt32(), "OFF-19-OUT slot " + slot);
            }
        }

        [TestCase("RNG-06", "Bounded draws are rejection-sampled and never exceed the bound")]
        public static void BoundedDrawsStayInRange()
        {
            Pcg32 rng = new Pcg32(12345UL, IndexedRng.CardDomain);
            for (int i = 0; i < 5000; i++)
            {
                uint value = rng.NextBounded(100);
                if (value >= 100)
                {
                    throw new AssertionException("Bounded draw returned " + value + ", which is outside [0,100).");
                }
            }
        }

        [TestCase("RNG-07", "A bound of 1 always yields 0 and a bound of 0 is rejected")]
        public static void BoundedEdgeCases()
        {
            Pcg32 rng = new Pcg32(7UL, IndexedRng.CardDomain);
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(0u, rng.NextBounded(1), "bound 1");
            }

            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    Pcg32 local = new Pcg32(7UL, IndexedRng.CardDomain);
                    local.NextBounded(0);
                },
                "A zero bound must be rejected, not silently divided by.");
        }

        [TestCase("SEED-01", "Crockford Base32 decodes canonically, ignoring dashes and case")]
        public static void SeedDecoding()
        {
            Assert.Equal(0UL, SeedCodec.Decode("0"), "zero");
            Assert.Equal(1UL, SeedCodec.Decode("1"), "one");

            // Dashes are ignored and the alphabet is case-insensitive on input.
            Assert.Equal(SeedCodec.Decode("K7QMRA92"), SeedCodec.Decode("K7-QMRA-92"), "dashes ignored");
            Assert.Equal(SeedCodec.Decode("K7QMRA92"), SeedCodec.Decode("k7qmra92"), "case insensitive");

            // Aliases: O maps to 0, I and L map to 1.
            Assert.Equal(SeedCodec.Decode("0"), SeedCodec.Decode("O"), "O is 0");
            Assert.Equal(SeedCodec.Decode("1"), SeedCodec.Decode("I"), "I is 1");
            Assert.Equal(SeedCodec.Decode("1"), SeedCodec.Decode("L"), "L is 1");
        }

        [TestCase("SEED-02", "Encoding is the inverse of decoding and emits no aliases")]
        public static void SeedRoundTrip()
        {
            ulong[] values = { 0UL, 1UL, 31UL, 32UL, 1234567890UL, ulong.MaxValue };
            foreach (ulong value in values)
            {
                string text = SeedCodec.Encode(value);
                Assert.Equal(value, SeedCodec.Decode(text), "round trip of " + text);
                if (text.IndexOfAny(new[] { 'O', 'I', 'L', 'U' }) >= 0)
                {
                    throw new AssertionException("Encoded seed '" + text + "' contains an excluded letter.");
                }
            }
        }

        [TestCase("SEED-03", "Seed text longer than 64 bits, or outside the alphabet, is rejected")]
        public static void SeedOverflowAndBadCharacters()
        {
            // ulong.MaxValue encodes to 13 digits; a 14th digit cannot fit in 64 bits.
            Assert.Throws<FormatException>(
                () => SeedCodec.Decode("ZZZZZZZZZZZZZZ"), "14 digits overflows 64 bits");
            Assert.Throws<FormatException>(() => SeedCodec.Decode("U"), "U is not in the alphabet");
            Assert.Throws<FormatException>(() => SeedCodec.Decode("-"), "no digits at all");
        }
    }
}
