using System;
using Epoch.Core.Domain;

namespace Epoch.Core.Content
{
    /// <summary>
    /// The locked Age progression (MR sec.2.2 / Core Spec sec.2.3). A static lookup
    /// table, never mutable state.
    ///
    /// **Do not rebalance these values.** MR sec.2.2 defends the x1.19 power step against
    /// an explicit alternative and accepts its cost.
    ///
    /// Costs are never authored on cards: BUILD, TRAIN and ADVANCE all read the
    /// resolving Age from here (Core Spec sec.5.10).
    /// </summary>
    public static class AgeTable
    {
        public static readonly AgeRow Dawn = new AgeRow(1, AgeName.DAWN, 1, 6, 2, 10, 6, 8, 1, 1);

        public static readonly AgeRow Bronze = new AgeRow(2, AgeName.BRONZE, 7, 12, 3, 12, 8, 11, 2, 2);

        public static readonly AgeRow Steel = new AgeRow(3, AgeName.STEEL, 13, 18, 5, 14, 11, 16, 3, 3);

        public static readonly AgeRow Modern = new AgeRow(4, AgeName.MODERN, 19, 24, 9, 17, 16, 22, 4, 4);

        private static readonly AgeRow[] Rows = { Dawn, Bronze, Steel, Modern };

        public static AgeRow ForAge(int age)
        {
            if (age < 1 || age > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(age), "Age is 1..4 (Core Spec sec.2.3).");
            }

            return Rows[age - 1];
        }

        /// <summary>Invariant RS-1 [Lock 1]: Age is a pure function of turn; nothing accelerates it.</summary>
        public static AgeRow ForTurn(int turn) => ForAge(RunState.AgeForTurn(turn));
    }

    public enum AgeName
    {
        DAWN,
        BRONZE,
        STEEL,
        MODERN,
    }

    /// <summary>Core Spec sec.5.5 AgeState.</summary>
    public sealed record AgeRow(
        int Index,
        AgeName Name,
        int FirstTurn,
        int LastTurn,
        int PerkCost,
        int UnitPower,
        int UnitCost,
        int BuildCost,
        int StructureTier,
        int TierYield)
    {
        /// <summary>The cost of a card type in this Age, in that type's own resource (Core Spec sec.7.1 E11).</summary>
        public int CostFor(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.BUILD:
                    return BuildCost;
                case CardType.TRAIN:
                    return UnitCost;
                case CardType.ADVANCE:
                    return PerkCost;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cardType));
            }
        }
    }
}
