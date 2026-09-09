using System;
using System.Collections.Generic;

namespace Epoch.Content.Json
{
    public enum JsonKind
    {
        Object,
        Array,
        String,
        Number,
        Boolean,
        Null,
    }

    /// <summary>
    /// A minimal, order-preserving JSON value.
    ///
    /// <para>Hand-rolled rather than <c>System.Text.Json</c> because that type is not in the
    /// netstandard2.1 surface and Implementation Lock decision (B) forbids NuGet packages
    /// in the simulation assemblies. Writing the reader also makes "strict" enforceable
    /// rather than aspirational: duplicate keys, trailing commas and unknown members are
    /// all rejected here instead of being quietly tolerated by a permissive
    /// general-purpose parser.</para>
    ///
    /// <para><b>Numbers never become floating point.</b> The raw lexeme is kept and
    /// converted on demand to <see cref="int"/> or to fixed-point hundredths by integer
    /// arithmetic. A content file cannot introduce a float into the authoritative path
    /// even by authoring one.</para>
    /// </summary>
    public sealed class JsonValue
    {
        private readonly List<string> _memberOrder;
        private readonly Dictionary<string, JsonValue> _members;
        private readonly List<JsonValue> _items;
        private readonly string _text;
        private readonly bool _boolean;

        private JsonValue(
            JsonKind kind,
            List<string>? memberOrder = null,
            Dictionary<string, JsonValue>? members = null,
            List<JsonValue>? items = null,
            string? text = null,
            bool boolean = false)
        {
            Kind = kind;
            _memberOrder = memberOrder ?? new List<string>();
            _members = members ?? new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            _items = items ?? new List<JsonValue>();
            _text = text ?? string.Empty;
            _boolean = boolean;
        }

        public JsonKind Kind { get; }

        /// <summary>The path from the document root, e.g. <c>perks[3].effects[0].magnitude</c>.</summary>
        public string Path { get; internal set; } = "$";

        internal static JsonValue NewObject(List<string> order, Dictionary<string, JsonValue> members) =>
            new JsonValue(JsonKind.Object, order, members);

        internal static JsonValue NewArray(List<JsonValue> items) =>
            new JsonValue(JsonKind.Array, items: items);

        internal static JsonValue NewString(string text) =>
            new JsonValue(JsonKind.String, text: text);

        internal static JsonValue NewNumber(string lexeme) =>
            new JsonValue(JsonKind.Number, text: lexeme);

        internal static JsonValue NewBoolean(bool value) =>
            new JsonValue(JsonKind.Boolean, boolean: value);

        internal static JsonValue NewNull() => new JsonValue(JsonKind.Null);

        public IReadOnlyList<string> MemberNames => _memberOrder;

        public IReadOnlyList<JsonValue> Items
        {
            get
            {
                Expect(JsonKind.Array);
                return _items;
            }
        }

        public bool Has(string name) => Kind == JsonKind.Object && _members.ContainsKey(name);

        public JsonValue Member(string name)
        {
            Expect(JsonKind.Object);
            if (!_members.TryGetValue(name, out JsonValue? value))
            {
                throw new ContentFormatException(Path, "required member '" + name + "' is missing");
            }

            return value;
        }

        public JsonValue? OptionalMember(string name)
        {
            Expect(JsonKind.Object);
            return _members.TryGetValue(name, out JsonValue? value) ? value : null;
        }

        /// <summary>
        /// Strictness gate: every member of this object must be one the loader knows.
        /// An unrecognised key is a schema drift the loader must not silently ignore
        /// (Technical Plan sec.14, "Content/hash drift").
        /// </summary>
        public void RejectUnknownMembers(params string[] known)
        {
            Expect(JsonKind.Object);
            for (int i = 0; i < _memberOrder.Count; i++)
            {
                string name = _memberOrder[i];
                bool recognised = false;
                for (int k = 0; k < known.Length; k++)
                {
                    if (string.Equals(known[k], name, StringComparison.Ordinal))
                    {
                        recognised = true;
                        break;
                    }
                }

                if (!recognised)
                {
                    throw new ContentFormatException(Path, "unknown member '" + name + "'");
                }
            }
        }

        public string AsString()
        {
            Expect(JsonKind.String);
            return _text;
        }

        public bool AsBoolean()
        {
            Expect(JsonKind.Boolean);
            return _boolean;
        }

        public bool IsNull => Kind == JsonKind.Null;

        /// <summary>The raw numeric lexeme, exactly as authored. Used for canonical hashing.</summary>
        public string NumberLexeme
        {
            get
            {
                Expect(JsonKind.Number);
                return _text;
            }
        }

        public int AsInt32()
        {
            Expect(JsonKind.Number);
            if (!TryParseIntegral(_text, out long value))
            {
                throw new ContentFormatException(Path, "expected an integer but found '" + _text + "'");
            }

            if (value < int.MinValue || value > int.MaxValue)
            {
                throw new ContentFormatException(Path, "integer '" + _text + "' does not fit in 32 bits");
            }

            return (int)value;
        }

        /// <summary>
        /// The number as fixed-point hundredths, by integer arithmetic only.
        /// <c>2</c> becomes 200, <c>1.5</c> becomes 150. More than two decimal places is a
        /// content error rather than a silent rounding, because a magnitude the engine
        /// cannot represent exactly is a balance value nobody authored.
        /// </summary>
        public int AsHundredths()
        {
            Expect(JsonKind.Number);

            string text = _text;
            if (text.IndexOf('e') >= 0 || text.IndexOf('E') >= 0)
            {
                throw new ContentFormatException(Path, "exponent notation is not accepted for a magnitude");
            }

            bool negative = text.Length > 0 && text[0] == '-';
            if (negative)
            {
                text = text.Substring(1);
            }

            int dot = text.IndexOf('.');
            string wholeText = dot < 0 ? text : text.Substring(0, dot);
            string fractionText = dot < 0 ? string.Empty : text.Substring(dot + 1);

            if (fractionText.Length > 2)
            {
                throw new ContentFormatException(
                    Path, "magnitude '" + _text + "' has more than two decimal places");
            }

            if (!TryParseIntegral(wholeText, out long whole))
            {
                throw new ContentFormatException(Path, "malformed number '" + _text + "'");
            }

            int fraction = 0;
            if (fractionText.Length == 1)
            {
                fraction = (fractionText[0] - '0') * 10;
            }
            else if (fractionText.Length == 2)
            {
                fraction = ((fractionText[0] - '0') * 10) + (fractionText[1] - '0');
            }

            long hundredths = (whole * 100) + fraction;
            if (negative)
            {
                hundredths = -hundredths;
            }

            if (hundredths < int.MinValue || hundredths > int.MaxValue)
            {
                throw new ContentFormatException(Path, "magnitude '" + _text + "' is out of range");
            }

            return (int)hundredths;
        }

        private static bool TryParseIntegral(string text, out long value)
        {
            value = 0;
            if (text.Length == 0)
            {
                return false;
            }

            bool negative = text[0] == '-';
            int index = negative ? 1 : 0;
            if (index >= text.Length)
            {
                return false;
            }

            long accumulated = 0;
            for (; index < text.Length; index++)
            {
                char c = text[index];
                if (c < '0' || c > '9')
                {
                    return false;
                }

                accumulated = (accumulated * 10) + (c - '0');
                if (accumulated > 4611686018427387903L)
                {
                    return false;
                }
            }

            value = negative ? -accumulated : accumulated;
            return true;
        }

        private void Expect(JsonKind kind)
        {
            if (Kind != kind)
            {
                throw new ContentFormatException(Path, "expected " + kind + " but found " + Kind);
            }
        }
    }

    /// <summary>
    /// A malformed or schema-violating content document. Always names the JSON path, so a
    /// failure says which entry is wrong rather than only that something is.
    /// </summary>
    public sealed class ContentFormatException : Exception
    {
        public ContentFormatException(string path, string problem)
            : base("Content error at " + path + ": " + problem + ".")
        {
            Path = path;
            Problem = problem;
        }

        public string Path { get; }

        public string Problem { get; }
    }
}
