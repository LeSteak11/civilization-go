using System;

namespace Epoch.Core.Domain
{
    /// <summary>
    /// Stable string key (Core Spec sec.5: "id"). A value type so that an id can never be
    /// confused with arbitrary display text, and so ordering is explicit and ordinal.
    /// Ordinal comparison is required: every tie-break chain in the spec ends in
    /// "ascending stable id", and that order must not vary by culture.
    /// </summary>
    public readonly struct StableId : IEquatable<StableId>, IComparable<StableId>
    {
        private readonly string? _value;

        public StableId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("A StableId may not be null or empty.", nameof(value));
            }

            _value = value;
        }

        public string Value => _value ?? string.Empty;

        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public bool Equals(StableId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is StableId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

        public int CompareTo(StableId other) => string.CompareOrdinal(Value, other.Value);

        public override string ToString() => Value;

        public static bool operator ==(StableId left, StableId right) => left.Equals(right);

        public static bool operator !=(StableId left, StableId right) => !left.Equals(right);
    }

    /// <summary>The shareable seed code, e.g. "K7-QMRA-92" (Core Spec sec.5.15, sec.10.2).</summary>
    public readonly struct SeedCode : IEquatable<SeedCode>
    {
        private readonly string? _text;

        public SeedCode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("A SeedCode may not be null or empty.", nameof(text));
            }

            _text = text;
        }

        public string Text => _text ?? string.Empty;

        public bool Equals(SeedCode other) => string.Equals(Text, other.Text, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is SeedCode other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Text);

        public override string ToString() => Text;

        public static bool operator ==(SeedCode left, SeedCode right) => left.Equals(right);

        public static bool operator !=(SeedCode left, SeedCode right) => !left.Equals(right);
    }
}
