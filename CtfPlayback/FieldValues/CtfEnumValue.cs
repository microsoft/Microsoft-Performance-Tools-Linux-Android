// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata;
using CtfPlayback.Metadata.Helpers;

namespace CtfPlayback.FieldValues
{
    /// <summary>
    /// Represents an enum field from a CTF event.
    /// </summary>
    public class CtfEnumValue
        : CtfFieldValue
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">string value of the enum</param>
        /// <param name="integerValue">the enum base integer type</param>
        public CtfEnumValue(string value, IntegerLiteral integerValue)
            : base(CtfTypes.Enum)
        {
            this.Value = string.Intern(value);
            this.IntegerValue = integerValue;
        }

        /// <summary>
        /// This is the value the base integer mapped to for the enum.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// This was the integer value that mapped to the enum value.
        /// </summary>
        public IntegerLiteral IntegerValue { get; }

        /// <summary>
        /// Returns a string representation of the field value.
        /// Useful when text dumping the contents of an event stream.
        /// </summary>
        /// <returns>The field value as a string</returns>
        public override string GetValueAsString(GetValueAsStringOptions options = GetValueAsStringOptions.NoOptions)
        {
            return Value;
        }
    }
}