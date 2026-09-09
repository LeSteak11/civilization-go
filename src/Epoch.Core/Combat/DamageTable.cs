using System;

namespace Epoch.Core.Combat
{
    /// <summary>
    /// The versioned 81-entry integer damage table, Delta in [-40, +40]
    /// (Core Spec sec.9.3, [Lock 7]).
    ///
    /// Generated offline from <c>25 * exp(0.025 * deltaPower)</c>, rounded half away
    /// from zero. Platform-native <c>exp()</c> must not be called during authoritative
    /// match resolution, so the values are checked in rather than computed — the
    /// architecture guard bans System.Math from the Core to make that unforgeable.
    ///
    /// The same 81 integers live in <c>fixtures/damage_table.json</c>; FIX-08 asserts the
    /// two agree, so a drift in either is a failing test rather than a silent rules change.
    /// </summary>
    public static class DamageTable
    {
        public const int DeltaMin = -40;

        public const int DeltaMax = 40;

        public const int EntryCount = 81;

        /// <summary>Minimum damage, at Delta = -40. DERIVED, Core Spec sec.9.3.</summary>
        public const int MinDamage = 9;

        /// <summary>Maximum damage, at Delta = +40. DERIVED, Core Spec sec.9.3.</summary>
        public const int MaxDamage = 68;

        private static readonly int[] DamageByDelta =
        {
            9, 9, 10, 10, 10, 10, 11, 11, 11, 12,
            12, 12, 12, 13, 13, 13, 14, 14, 14, 15,
            15, 16, 16, 16, 17, 17, 18, 18, 19, 19,
            19, 20, 20, 21, 22, 22, 23, 23, 24, 24,
            25, 26, 26, 27, 28, 28, 29, 30, 31, 31,
            32, 33, 34, 35, 35, 36, 37, 38, 39, 40,
            41, 42, 43, 44, 46, 47, 48, 49, 50, 52,
            53, 54, 56, 57, 58, 60, 61, 63, 65, 66,
            68,
        };

        /// <summary>
        /// Read damage for an already-clamped, already-rounded integer Delta.
        /// Passing an out-of-range Delta is a programming error, not a game state:
        /// the caller must clamp at Core Spec sec.9.1 C7 before reaching here.
        /// </summary>
        public static int ForDelta(int deltaPower)
        {
            if (deltaPower < DeltaMin || deltaPower > DeltaMax)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaPower),
                    "Delta Power must be clamped to [-40, +40] before the table read (Core Spec sec.9.1).");
            }

            return DamageByDelta[deltaPower - DeltaMin];
        }

        /// <summary>A copy, so no caller can mutate the locked table.</summary>
        public static int[] ToArray()
        {
            int[] copy = new int[EntryCount];
            Array.Copy(DamageByDelta, copy, EntryCount);
            return copy;
        }
    }
}
