using System;
using System.Collections.Generic;
using System.Text;

namespace Epoch.Content.Json
{
    /// <summary>
    /// A strict RFC 8259 reader. Everything it refuses, it refuses on purpose:
    ///
    /// <list type="bullet">
    /// <item>duplicate object keys - the later one would silently win in most parsers,
    /// making two different documents load identically</item>
    /// <item>trailing commas, comments, single quotes, unquoted keys - all common
    /// hand-edit artefacts that should fail loudly in a content pipeline</item>
    /// <item>NaN, Infinity, leading <c>+</c>, leading zeros, a bare leading <c>.</c></item>
    /// <item>raw control characters inside strings</item>
    /// <item>trailing content after the top-level value</item>
    /// </list>
    ///
    /// It never converts a number to a floating-point type; the lexeme is preserved and
    /// converted by integer arithmetic on demand (<see cref="JsonValue.AsHundredths"/>).
    /// </summary>
    public static class JsonReader
    {
        /// <summary>Guards against a hand-edited file with runaway nesting.</summary>
        private const int MaxDepth = 64;

        public static JsonValue Parse(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            Cursor cursor = new Cursor(text);

            // A UTF-8 BOM is legal in a file but not part of the JSON grammar.
            if (cursor.Remaining > 0 && cursor.Peek() == (char)0xFEFF)
            {
                cursor.Advance();
            }

            SkipWhitespace(ref cursor);
            JsonValue value = ParseValue(ref cursor, "$", 0);
            SkipWhitespace(ref cursor);

            if (cursor.Remaining > 0)
            {
                throw new ContentFormatException("$", "unexpected trailing content after the top-level value");
            }

            return value;
        }

        private static JsonValue ParseValue(ref Cursor cursor, string path, int depth)
        {
            if (depth > MaxDepth)
            {
                throw new ContentFormatException(path, "nesting is deeper than " + MaxDepth + " levels");
            }

            if (cursor.Remaining == 0)
            {
                throw new ContentFormatException(path, "unexpected end of document");
            }

            char c = cursor.Peek();
            switch (c)
            {
                case '{':
                    return ParseObject(ref cursor, path, depth);
                case '[':
                    return ParseArray(ref cursor, path, depth);
                case '"':
                    return Tag(JsonValue.NewString(ParseString(ref cursor, path)), path);
                case 't':
                    ExpectLiteral(ref cursor, "true", path);
                    return Tag(JsonValue.NewBoolean(true), path);
                case 'f':
                    ExpectLiteral(ref cursor, "false", path);
                    return Tag(JsonValue.NewBoolean(false), path);
                case 'n':
                    ExpectLiteral(ref cursor, "null", path);
                    return Tag(JsonValue.NewNull(), path);
                default:
                    return Tag(JsonValue.NewNumber(ParseNumber(ref cursor, path)), path);
            }
        }

        private static JsonValue ParseObject(ref Cursor cursor, string path, int depth)
        {
            cursor.Advance(); // '{'
            List<string> order = new List<string>();
            Dictionary<string, JsonValue> members = new Dictionary<string, JsonValue>(StringComparer.Ordinal);

            SkipWhitespace(ref cursor);
            if (cursor.Remaining > 0 && cursor.Peek() == '}')
            {
                cursor.Advance();
                return Tag(JsonValue.NewObject(order, members), path);
            }

            while (true)
            {
                SkipWhitespace(ref cursor);
                if (cursor.Remaining == 0 || cursor.Peek() != '"')
                {
                    throw new ContentFormatException(path, "expected a quoted member name");
                }

                string name = ParseString(ref cursor, path);
                if (members.ContainsKey(name))
                {
                    throw new ContentFormatException(path, "duplicate member '" + name + "'");
                }

                SkipWhitespace(ref cursor);
                if (cursor.Remaining == 0 || cursor.Peek() != ':')
                {
                    throw new ContentFormatException(path, "expected ':' after member '" + name + "'");
                }

                cursor.Advance();
                SkipWhitespace(ref cursor);

                string childPath = path == "$" ? name : path + "." + name;
                JsonValue value = ParseValue(ref cursor, childPath, depth + 1);

                order.Add(name);
                members.Add(name, value);

                SkipWhitespace(ref cursor);
                if (cursor.Remaining == 0)
                {
                    throw new ContentFormatException(path, "unterminated object");
                }

                char next = cursor.Peek();
                if (next == ',')
                {
                    cursor.Advance();
                    SkipWhitespace(ref cursor);
                    if (cursor.Remaining > 0 && cursor.Peek() == '}')
                    {
                        throw new ContentFormatException(path, "trailing comma before '}'");
                    }

                    continue;
                }

                if (next == '}')
                {
                    cursor.Advance();
                    return Tag(JsonValue.NewObject(order, members), path);
                }

                throw new ContentFormatException(path, "expected ',' or '}'");
            }
        }

        private static JsonValue ParseArray(ref Cursor cursor, string path, int depth)
        {
            cursor.Advance(); // '['
            List<JsonValue> items = new List<JsonValue>();

            SkipWhitespace(ref cursor);
            if (cursor.Remaining > 0 && cursor.Peek() == ']')
            {
                cursor.Advance();
                return Tag(JsonValue.NewArray(items), path);
            }

            while (true)
            {
                SkipWhitespace(ref cursor);
                string childPath = path + "[" + items.Count + "]";
                items.Add(ParseValue(ref cursor, childPath, depth + 1));

                SkipWhitespace(ref cursor);
                if (cursor.Remaining == 0)
                {
                    throw new ContentFormatException(path, "unterminated array");
                }

                char next = cursor.Peek();
                if (next == ',')
                {
                    cursor.Advance();
                    SkipWhitespace(ref cursor);
                    if (cursor.Remaining > 0 && cursor.Peek() == ']')
                    {
                        throw new ContentFormatException(path, "trailing comma before ']'");
                    }

                    continue;
                }

                if (next == ']')
                {
                    cursor.Advance();
                    return Tag(JsonValue.NewArray(items), path);
                }

                throw new ContentFormatException(path, "expected ',' or ']'");
            }
        }

        private static string ParseString(ref Cursor cursor, string path)
        {
            cursor.Advance(); // opening quote
            StringBuilder text = new StringBuilder();

            while (true)
            {
                if (cursor.Remaining == 0)
                {
                    throw new ContentFormatException(path, "unterminated string");
                }

                char c = cursor.Read();
                if (c == '"')
                {
                    return text.ToString();
                }

                if (c == '\\')
                {
                    if (cursor.Remaining == 0)
                    {
                        throw new ContentFormatException(path, "unterminated escape sequence");
                    }

                    char escape = cursor.Read();
                    switch (escape)
                    {
                        case '"': text.Append('"'); break;
                        case '\\': text.Append('\\'); break;
                        case '/': text.Append('/'); break;
                        case 'b': text.Append('\b'); break;
                        case 'f': text.Append('\f'); break;
                        case 'n': text.Append('\n'); break;
                        case 'r': text.Append('\r'); break;
                        case 't': text.Append('\t'); break;
                        case 'u': text.Append(ParseUnicodeEscape(ref cursor, path)); break;
                        default:
                            throw new ContentFormatException(path, "invalid escape '\\" + escape + "'");
                    }

                    continue;
                }

                if (c < 0x20)
                {
                    throw new ContentFormatException(path, "raw control character in string");
                }

                text.Append(c);
            }
        }

        private static char ParseUnicodeEscape(ref Cursor cursor, string path)
        {
            if (cursor.Remaining < 4)
            {
                throw new ContentFormatException(path, "truncated \\u escape");
            }

            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                char c = cursor.Read();
                int digit;
                if (c >= '0' && c <= '9')
                {
                    digit = c - '0';
                }
                else if (c >= 'a' && c <= 'f')
                {
                    digit = (c - 'a') + 10;
                }
                else if (c >= 'A' && c <= 'F')
                {
                    digit = (c - 'A') + 10;
                }
                else
                {
                    throw new ContentFormatException(path, "invalid hex digit in \\u escape");
                }

                value = (value * 16) + digit;
            }

            return (char)value;
        }

        private static string ParseNumber(ref Cursor cursor, string path)
        {
            int start = cursor.Position;

            if (cursor.Remaining > 0 && cursor.Peek() == '-')
            {
                cursor.Advance();
            }

            if (cursor.Remaining == 0 || !IsDigit(cursor.Peek()))
            {
                throw new ContentFormatException(path, "expected a number");
            }

            // No leading zeros: "01" is not a JSON number.
            if (cursor.Peek() == '0')
            {
                cursor.Advance();
                if (cursor.Remaining > 0 && IsDigit(cursor.Peek()))
                {
                    throw new ContentFormatException(path, "numbers may not have a leading zero");
                }
            }
            else
            {
                while (cursor.Remaining > 0 && IsDigit(cursor.Peek()))
                {
                    cursor.Advance();
                }
            }

            if (cursor.Remaining > 0 && cursor.Peek() == '.')
            {
                cursor.Advance();
                if (cursor.Remaining == 0 || !IsDigit(cursor.Peek()))
                {
                    throw new ContentFormatException(path, "expected a digit after the decimal point");
                }

                while (cursor.Remaining > 0 && IsDigit(cursor.Peek()))
                {
                    cursor.Advance();
                }
            }

            if (cursor.Remaining > 0 && (cursor.Peek() == 'e' || cursor.Peek() == 'E'))
            {
                cursor.Advance();
                if (cursor.Remaining > 0 && (cursor.Peek() == '+' || cursor.Peek() == '-'))
                {
                    cursor.Advance();
                }

                if (cursor.Remaining == 0 || !IsDigit(cursor.Peek()))
                {
                    throw new ContentFormatException(path, "expected a digit in the exponent");
                }

                while (cursor.Remaining > 0 && IsDigit(cursor.Peek()))
                {
                    cursor.Advance();
                }
            }

            return cursor.Slice(start);
        }

        private static void ExpectLiteral(ref Cursor cursor, string literal, string path)
        {
            if (cursor.Remaining < literal.Length)
            {
                throw new ContentFormatException(path, "expected '" + literal + "'");
            }

            for (int i = 0; i < literal.Length; i++)
            {
                if (cursor.PeekAt(i) != literal[i])
                {
                    throw new ContentFormatException(path, "expected '" + literal + "'");
                }
            }

            cursor.Advance(literal.Length);
        }

        private static void SkipWhitespace(ref Cursor cursor)
        {
            // Exactly the four characters the grammar allows. A comment or a stray
            // non-breaking space is an error, not whitespace.
            while (cursor.Remaining > 0)
            {
                char c = cursor.Peek();
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                {
                    cursor.Advance();
                    continue;
                }

                return;
            }
        }

        private static bool IsDigit(char c) => c >= '0' && c <= '9';

        private static JsonValue Tag(JsonValue value, string path)
        {
            value.Path = path;
            return value;
        }

        private struct Cursor
        {
            private readonly string _text;

            internal Cursor(string text)
            {
                _text = text;
                Position = 0;
            }

            internal int Position { get; private set; }

            internal int Remaining => _text.Length - Position;

            internal char Peek() => _text[Position];

            internal char PeekAt(int offset) => _text[Position + offset];

            internal char Read() => _text[Position++];

            internal void Advance(int count = 1) => Position += count;

            internal string Slice(int start) => _text.Substring(start, Position - start);
        }
    }
}
