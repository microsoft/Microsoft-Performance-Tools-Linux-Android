// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata;

namespace CtfPlayback.FieldValues
{
    /// <summary>
    /// Represents a string field from a CTF event
    /// </summary>
    public class CtfStringValue
        : CtfFieldValue
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">Field value</param>
        public CtfStringValue(string value)
            : base(CtfTypes.String)
        {
            this.Value = value;
        }

        /// <summary>
        /// Field value
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Returns a string representation of the field value.
        /// Useful when text dumping the contents of an event stream.
        /// </summary>
        /// <returns>The field value as a string</returns>
        public override string GetValueAsString(GetValueAsStringOptions options = GetValueAsStringOptions.NoOptions)
        {
            if (options.HasFlag(GetValueAsStringOptions.QuotesAroundStrings))
            {
                return '"' + Value + '"';
            }

            return Value;
        }
    }
}