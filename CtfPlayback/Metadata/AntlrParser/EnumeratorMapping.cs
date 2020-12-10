// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Helpers;

namespace CtfPlayback.Metadata.AntlrParser
{
    internal class EnumeratorMapping
    {
        internal string EnumIdentifier { get; set; }

        internal IntegerLiteral StartingValue { get; set; }

        internal IntegerLiteral EndingValue { get; set; }

        public override string ToString()
        {
            if (this.EndingValue != null && this.EndingValue != this.StartingValue)
            {
                return $"EnumIdentifier = {this.StartingValue} ... {this.EndingValue}";
            }

            return $"EnumIdentifier = {this.StartingValue}";
        }
    }
}