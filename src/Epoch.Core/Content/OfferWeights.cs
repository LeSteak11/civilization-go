using System;

namespace Epoch.Core.Content
{
    /// <summary>
    /// The V1 **effective** deck weighting by turn band (Core Spec sec.2.6).
    ///
    /// The turn 19-20 and 21-24 rows supersede MR sec.1.6 under the V1 Build Economy
    /// Exception (sec.6.7) and [Final Lock 2]: BUILD is offered on turns 1-20 and has
    /// 0% weight on turns 21-24. Percentages sum to 100 in every band.
    /// </summary>
    public static class OfferWeights
    {
        private static readonly OfferWeightBand[] Bands =
        {
            new OfferWeightBand(1, 3, 70, 20, 10),
            new OfferWeightBand(4, 6, 45, 35, 20),
            new OfferWeightBand(7, 12, 35, 40, 25),
            new OfferWeightBand(13, 18, 20, 45, 35),
            new OfferWeightBand(19, 20, 20, 50, 30),
            new OfferWeightBand(21, 24, 0, 60, 40),
        };

        /// <summary>The last turn on which a BUILD may be offered (V1 Build Economy Exception X3).</summary>
        public const int BuildOfferLastTurn = 20;

        public static OfferWeightBand ForTurn(int turn)
        {
            for (int i = 0; i < Bands.Length; i++)
            {
                if (turn >= Bands[i].FirstTurn && turn <= Bands[i].LastTurn)
                {
                    return Bands[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(turn), "Turn is 1..24 (Core Spec sec.2.1).");
        }

        public static OfferWeightBand[] AllBands()
        {
            OfferWeightBand[] copy = new OfferWeightBand[Bands.Length];
            Array.Copy(Bands, copy, Bands.Length);
            return copy;
        }
    }

    public sealed record OfferWeightBand(int FirstTurn, int LastTurn, int Build, int Train, int Advance)
    {
        public int Total => Build + Train + Advance;
    }
}
