// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CtfPlayback.Metadata.Ctf2
{
    /// <summary>
    /// A JSON number, kept in its textual form so that 64-bit unsigned values are not truncated.
    /// </summary>
    internal sealed class Ctf2JsonNumber
    {
        internal Ctf2JsonNumber(string text)
        {
            this.Text = text;
        }

        internal string Text { get; }

        internal bool IsNegative => this.Text.StartsWith("-", StringComparison.Ordinal);

        internal long AsLong() => long.Parse(this.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

        internal ulong AsULong() => ulong.Parse(this.Text, NumberStyles.None, CultureInfo.InvariantCulture);

        public override string ToString() => this.Text;
    }

    /// <summary>
    /// Minimal JSON reader used for CTF 2 metadata. Objects are returned as
    /// <see cref="Dictionary{TKey,TValue}"/> of string to object, arrays as <see cref="List{T}"/> of object,
    /// numbers as <see cref="Ctf2JsonNumber"/>, and strings, booleans and null as their .NET equivalents.
    /// </summary>
    internal sealed class Ctf2Json
    {
        private readonly string text;
        private int position;

        private Ctf2Json(string text)
        {
            this.text = text;
        }

        internal static object Parse(string text)
        {
            var reader = new Ctf2Json(text);
            reader.SkipWhitespace();
            var value = reader.ReadValue();
            reader.SkipWhitespace();
            if (reader.position != text.Length)
            {
                throw reader.Error("Unexpected trailing characters");
            }

            return value;
        }

        private object ReadValue()
        {
            if (this.position >= this.text.Length)
            {
                throw this.Error("Unexpected end of JSON");
            }

            char c = this.text[this.position];
            switch (c)
            {
                case '{':
                    return this.ReadObject();
                case '[':
                    return this.ReadArray();
                case '"':
                    return this.ReadString();
                case 't':
                    this.Expect("true");
                    return true;
                case 'f':
                    this.Expect("false");
                    return false;
                case 'n':
                    this.Expect("null");
                    return null;
                default:
                    if (c == '-' || (c >= '0' && c <= '9'))
                    {
                        return this.ReadNumber();
                    }

                    throw this.Error($"Unexpected character '{c}'");
            }
        }

        private Dictionary<string, object> ReadObject()
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            this.position++;
            this.SkipWhitespace();
            if (this.TryConsume('}'))
            {
                return result;
            }

            while (true)
            {
                this.SkipWhitespace();
                if (this.position >= this.text.Length || this.text[this.position] != '"')
                {
                    throw this.Error("Expected property name");
                }

                string name = this.ReadString();
                this.SkipWhitespace();
                if (!this.TryConsume(':'))
                {
                    throw this.Error("Expected ':'");
                }

                this.SkipWhitespace();
                result[name] = this.ReadValue();
                this.SkipWhitespace();

                if (this.TryConsume(','))
                {
                    continue;
                }

                if (this.TryConsume('}'))
                {
                    return result;
                }

                throw this.Error("Expected ',' or '}'");
            }
        }

        private List<object> ReadArray()
        {
            var result = new List<object>();
            this.position++;
            this.SkipWhitespace();
            if (this.TryConsume(']'))
            {
                return result;
            }

            while (true)
            {
                this.SkipWhitespace();
                result.Add(this.ReadValue());
                this.SkipWhitespace();

                if (this.TryConsume(','))
                {
                    continue;
                }

                if (this.TryConsume(']'))
                {
                    return result;
                }

                throw this.Error("Expected ',' or ']'");
            }
        }

        private string ReadString()
        {
            this.position++;
            var sb = new StringBuilder();
            while (this.position < this.text.Length)
            {
                char c = this.text[this.position++];
                if (c == '"')
                {
                    return sb.ToString();
                }

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (this.position >= this.text.Length)
                {
                    break;
                }

                char escaped = this.text[this.position++];
                switch (escaped)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (this.position + 4 > this.text.Length ||
                            !int.TryParse(this.text.Substring(this.position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int codeUnit))
                        {
                            throw this.Error("Invalid unicode escape");
                        }

                        sb.Append((char)codeUnit);
                        this.position += 4;
                        break;
                    default:
                        throw this.Error($"Invalid escape character '{escaped}'");
                }
            }

            throw this.Error("Unterminated string");
        }

        private Ctf2JsonNumber ReadNumber()
        {
            int start = this.position;
            while (this.position < this.text.Length)
            {
                char c = this.text[this.position];
                if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E')
                {
                    this.position++;
                    continue;
                }

                break;
            }

            return new Ctf2JsonNumber(this.text.Substring(start, this.position - start));
        }

        private void Expect(string literal)
        {
            if (string.CompareOrdinal(this.text, this.position, literal, 0, literal.Length) != 0)
            {
                throw this.Error($"Expected '{literal}'");
            }

            this.position += literal.Length;
        }

        private bool TryConsume(char c)
        {
            if (this.position < this.text.Length && this.text[this.position] == c)
            {
                this.position++;
                return true;
            }

            return false;
        }

        private void SkipWhitespace()
        {
            while (this.position < this.text.Length && char.IsWhiteSpace(this.text[this.position]))
            {
                this.position++;
            }
        }

        private CtfMetadataException Error(string message)
        {
            return new CtfMetadataException($"Invalid CTF 2 metadata JSON: {message} at offset {this.position}.");
        }
    }
}
