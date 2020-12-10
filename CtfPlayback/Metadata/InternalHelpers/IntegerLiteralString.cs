// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CtfPlayback.Metadata.InternalHelpers
{
    internal enum NumberRadixFormat
    {
        Decimal,
        Hexadecimal,
        Octal
    }

    internal class IntegerLiteralString
    {
        // According to specification 1.82 grammar on integer-suffix, only values marked with unsigned may have
        // long or long-long suffixes.
        //
        internal static readonly string DecimalRegExPattern = "^(?<PlainNumber>0|[1-9][0-9]*)(?i)((?<NumberSuffix>u|ul|ull|lu|llu))?$";
        internal static readonly string HexRegExPattern = "^(?<PlainNumber>0[xX][A-Fa-f0-9]+)(?i)((?<NumberSuffix>u|ul|ull|lu|llu))?$";
        internal static readonly string OctalRegExPattern = "^(?<PlainNumber>0[0-7]+)(?i)((?<NumberSuffix>u|ul|ull|lu|llu))?$";

        internal static readonly Regex DecimalRegEx = new Regex(DecimalRegExPattern);
        internal static readonly Regex HexRegEx = new Regex(HexRegExPattern);
        internal static readonly Regex OctalRegEx = new Regex(OctalRegExPattern);

        internal static bool TryCreate(string value, out IntegerLiteralString returnValue)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(value));

            returnValue = null;

            Match match = ParseString(value, DecimalRegEx);
            if (match != null)
            {
                returnValue = new IntegerLiteralString(match, NumberRadixFormat.Decimal);
                return true;
            }

            match = ParseString(value, HexRegEx);
            if (match != null)
            {
                returnValue = new IntegerLiteralString(match, NumberRadixFormat.Hexadecimal);
                return true;
            }

            match = ParseString(value, OctalRegEx);
            if (match != null)
            {
                returnValue = new IntegerLiteralString(match, NumberRadixFormat.Octal);
                return true;
            }

            return false;
        }

        internal IntegerLiteralString(string value)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(value));

            if (!this.ParseString(value, DecimalRegEx, NumberRadixFormat.Decimal))
            {
                if (!this.ParseString(value, HexRegEx, NumberRadixFormat.Hexadecimal))
                {
                    if (!this.ParseString(value, OctalRegEx, NumberRadixFormat.Octal))
                    {
                        throw new ArgumentException("The provided string is not a supported number format.", nameof(value));
                    }
                }
            }
        }

        internal IntegerLiteralString(string value, NumberRadixFormat radix)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(value));

            switch (radix)
            {
                case NumberRadixFormat.Decimal:
                    if (!this.ParseString(value, DecimalRegEx, radix))
                    {
                        throw new ArgumentException("The provided string is not a supported number format.", nameof(value));
                    }
                    break;

                case NumberRadixFormat.Hexadecimal:
                    if (!this.ParseString(value, HexRegEx, radix))
                    {
                        throw new ArgumentException("The provided string is not a supported number format.", nameof(value));
                    }
                    break;

                case NumberRadixFormat.Octal:
                    if (!this.ParseString(value, OctalRegEx, radix))
                    {
                        throw new ArgumentException("The provided string is not a supported number format.", nameof(value));
                    }
                    break;
            }
        }

        private IntegerLiteralString(Match match, NumberRadixFormat radix)
        {
            Debug.Assert(match != null);

            this.AssignMatch(match, radix);
        }

        internal string PlainNumber { get; private set; }

        internal string NumberSuffix { get; private set; }

        internal NumberRadixFormat Radix { get; private set; }

        internal int NumberBase
        {
            get
            {
                switch (this.Radix)
                {
                    case NumberRadixFormat.Decimal: return 10;
                    case NumberRadixFormat.Hexadecimal: return 16;
                    case NumberRadixFormat.Octal: return 8;
                }

                Debug.Assert(false, $"Missing {nameof(this.NumberBase)} conversion.");
                throw new InvalidOperationException($"A {nameof(NumberRadixFormat)} value is missing a {nameof(this.NumberBase)}.");
            }
        }

        private bool ParseString(string integerString, Regex regex, NumberRadixFormat radix)
        {
            var match = ParseString(integerString, regex);
            if (match == null)
            {
                return false;
            }

            this.AssignMatch(match, radix);

            return true;
        }

        private void AssignMatch(Match match, NumberRadixFormat radix)
        {
            Group plainNumberGroup = match.Groups["PlainNumber"];
            Group suffixGroup = match.Groups["NumberSuffix"];

            // just an assert, because if this happens, it's a bug with the regex
            Debug.Assert(plainNumberGroup.Success);

            this.PlainNumber = plainNumberGroup.Value;
            this.NumberSuffix = this.ParseIntegerSuffix(suffixGroup);
            this.Radix = radix;
        }

        private string ParseIntegerSuffix(Group suffixGroup)
        {
            if (!suffixGroup.Success)
            {
                return string.Empty;
            }

            return suffixGroup.Value;
        }

        private static Match ParseString(string integerString, Regex regex)
        {
            if (!regex.IsMatch(integerString))
            {
                // not a decimal string
                return null;
            }

            var matches = regex.Matches(integerString);
            if (matches.Count > 1)
            {
                // this string isn't a single number?
                return null;
            }

            return matches[0];
        }
    }
}