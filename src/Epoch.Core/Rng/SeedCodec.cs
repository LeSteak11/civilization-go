using System;
using Epoch.Core.Domain;

namespace Epoch.Core.Rng
{
    /// <summary>
    /// Canonical Crockford Base32 seed text (Core Spec sec.10.2, [Final Lock 5]).
    ///
    /// Case-insensitive on input, dashes ignored, O maps to 0 and I/L map to 1.
    /// After normalization the text holds 1-13 digits and must decode to an unsigned
    /// 64-bit value; overflow is invalid. The leftmost digit is most significant.
    /// Display encoding emits uppercase and never emits the aliases.
    /// </summary>
    public static class SeedCodec
    {
        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        /// <summary>13 digits x 5 bits = 65 bits, so 13 is the longest text that can fit 64 bits.</summary>
        private const int MaxDigits = 13;

        public static ulong Decode(SeedCode seed) => Decode(seed.Text);

        public static ulong Decode(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            ulong value = 0UL;
            int digits = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '-')
                {
                    continue;
                }

                int digit = DigitValue(c);
                if (digit < 0)
                {
                    throw new FormatException("Seed text contains a character that is not Crockford Base32: '" + c + "'.");
                }

                digits++;
                if (digits > MaxDigits)
                {
                    throw new FormatException("Seed text decodes to more than 64 bits.");
                }

                // Overflow guard: shifting left by 5 must not discard a set bit.
                if ((value >> 59) != 0UL)
                {
                    throw new FormatException("Seed text decodes to more than 64 bits.");
                }

                value = (value << 5) | (uint)digit;
            }

            if (digits == 0)
            {
                throw new FormatException("Seed text contains no Base32 digits.");
            }

            return value;
        }

        /// <summary>Uppercase canonical text, no dashes, no aliases. The inverse of <see cref="Decode(string)"/>.</summary>
        public static string Encode(ulong value)
        {
            if (value == 0UL)
            {
                return "0";
            }

            char[] buffer = new char[MaxDigits];
            int index = buffer.Length;
            while (value > 0UL)
            {
                buffer[--index] = Alphabet[(int)(value & 31UL)];
                value >>= 5;
            }

            return new string(buffer, index, buffer.Length - index);
        }

        private static int DigitValue(char c)
        {
            // Aliases first (Core Spec sec.10.2): O -> 0, I/L -> 1.
            switch (c)
            {
                case 'o':
                case 'O':
                    return 0;
                case 'i':
                case 'I':
                case 'l':
                case 'L':
                    return 1;
                default:
                    break;
            }

            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            char upper = c;
            if (upper >= 'a' && upper <= 'z')
            {
                upper = (char)(upper - ('a' - 'A'));
            }

            int index = Alphabet.IndexOf(upper);
            return index >= 10 ? index : -1;
        }
    }
}
