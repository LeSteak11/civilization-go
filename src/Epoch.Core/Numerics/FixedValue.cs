using System;

namespace Epoch.Core.Numerics
{
    /// <summary>
    /// Fixed-point value in hundredths.
    ///
    /// Power, effect magnitudes and every multiplier are held as integers so that no
    /// float ever reaches the authoritative path (Core Spec sec.10.4 "residual float
    /// risk", option (a) — the option the spec recommends; Implementation Lock 4).
    /// The architecture guard ARCH-03 enforces that no float appears in the Core's
    /// public surface, and this type is why that is possible.
    ///
    /// All arithmetic is exact for the value ranges V1 uses. Where a product cannot be
    /// represented exactly in hundredths, <see cref="ScaleByHundredths"/> rounds half
    /// away from zero — the same rule the spec applies to Delta Power [Lock 7].
    /// </summary>
    public readonly struct FixedValue : IEquatable<FixedValue>, IComparable<FixedValue>
    {
        /// <summary>Hundredths per whole unit.</summary>
        public const int Scale = 100;

        public FixedValue(int hundredths)
        {
            Hundredths = hundredths;
        }

        public int Hundredths { get; }

        public static FixedValue Zero => default;

        public static FixedValue FromInt(int whole) => new FixedValue(whole * Scale);

        /// <summary>The multiplier 1.00, expressed in hundredths.</summary>
        public const int One = 100;

        public static FixedValue operator +(FixedValue a, FixedValue b) =>
            new FixedValue(a.Hundredths + b.Hundredths);

        public static FixedValue operator -(FixedValue a, FixedValue b) =>
            new FixedValue(a.Hundredths - b.Hundredths);

        public static FixedValue operator -(FixedValue a) => new FixedValue(-a.Hundredths);

        public static bool operator <(FixedValue a, FixedValue b) => a.Hundredths < b.Hundredths;

        public static bool operator >(FixedValue a, FixedValue b) => a.Hundredths > b.Hundredths;

        public static bool operator <=(FixedValue a, FixedValue b) => a.Hundredths <= b.Hundredths;

        public static bool operator >=(FixedValue a, FixedValue b) => a.Hundredths >= b.Hundredths;

        public static FixedValue Add(FixedValue a, FixedValue b) => a + b;

        public static FixedValue Subtract(FixedValue a, FixedValue b) => a - b;

        public static FixedValue Negate(FixedValue a) => -a;

        /// <summary>
        /// Multiply by a factor itself expressed in hundredths: 100 = x1.00, 75 = x0.75,
        /// 140 = x1.40. This is how the soft-stacking multipliers (MR sec.2.3) and the
        /// counter bonus (Core Spec sec.9.2 C4) are applied without touching a float.
        /// </summary>
        public FixedValue ScaleByHundredths(int factorHundredths)
        {
            long product = (long)Hundredths * factorHundredths;
            return new FixedValue(checked((int)DivideRoundHalfAwayFromZero(product, Scale)));
        }

        /// <summary>Clamp into an inclusive range. Applied before rounding, per Core Spec sec.9.1.</summary>
        public FixedValue Clamp(FixedValue min, FixedValue max)
        {
            if (Hundredths < min.Hundredths)
            {
                return min;
            }

            return Hundredths > max.Hundredths ? max : this;
        }

        /// <summary>
        /// Round to a whole integer, halves away from zero [Lock 7]. This is the single
        /// point where fractional Power collapses to the integer Delta that indexes the
        /// damage table, and it is the reason the authoritative path is float-free.
        /// </summary>
        public int RoundHalfAwayFromZeroToInt() =>
            (int)DivideRoundHalfAwayFromZero(Hundredths, Scale);

        private static long DivideRoundHalfAwayFromZero(long numerator, long denominator)
        {
            long half = denominator / 2;
            return numerator >= 0
                ? (numerator + half) / denominator
                : -((-numerator + half) / denominator);
        }

        public bool Equals(FixedValue other) => Hundredths == other.Hundredths;

        public override bool Equals(object? obj) => obj is FixedValue other && Equals(other);

        public override int GetHashCode() => Hundredths;

        public int CompareTo(FixedValue other) => Hundredths.CompareTo(other.Hundredths);

        public static bool operator ==(FixedValue a, FixedValue b) => a.Equals(b);

        public static bool operator !=(FixedValue a, FixedValue b) => !a.Equals(b);

        /// <summary>
        /// Culture-invariant "w.hh" for canonical serialization and debug output.
        /// Never uses a locale-sensitive formatter: the Core has no locale.
        /// </summary>
        public override string ToString()
        {
            int whole = Hundredths / Scale;
            int frac = Hundredths % Scale;
            string sign = (Hundredths < 0 && whole == 0) ? "-" : string.Empty;
            if (frac < 0)
            {
                frac = -frac;
            }

            string fracText = frac < 10 ? "0" + IntToString(frac) : IntToString(frac);
            return sign + IntToString(whole) + "." + fracText;
        }

        /// <summary>
        /// Ordinal integer formatting without System.Globalization. Deliberate: the Core
        /// must produce identical text on every platform and under every locale, and the
        /// same guarantee is needed by anything that canonicalizes state or content.
        /// </summary>
        public static string IntToString(int value)
        {
            if (value == 0)
            {
                return "0";
            }

            bool negative = value < 0;
            // Accumulate in long so int.MinValue negates safely.
            long magnitude = negative ? -(long)value : value;
            char[] digits = new char[20];
            int index = digits.Length;
            while (magnitude > 0)
            {
                digits[--index] = (char)('0' + (int)(magnitude % 10));
                magnitude /= 10;
            }

            if (negative)
            {
                digits[--index] = '-';
            }

            return new string(digits, index, digits.Length - index);
        }
    }
}
