// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using CtfPlayback.Metadata;

namespace CtfPlayback.FieldValues
{
    /// <summary>
    /// Represents a float field from a CTF event
    /// </summary>
    public class CtfFloatValue
        : CtfFieldValue
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">Float value</param>
        public CtfFloatValue(float value)
            : base(CtfTypes.Float)
        {
            this.Value = value;
        }

        /// <summary>
        /// Field value
        /// </summary>
        public float Value { get; }

        /// <summary>
        /// Returns a string representation of the field value.
        /// Useful when text dumping the contents of an event stream.
        /// </summary>
        /// <returns>The field value as a string</returns>
        public override string GetValueAsString(GetValueAsStringOptions options = GetValueAsStringOptions.NoOptions)
        {
            return string.Intern(Value.ToString(CultureInfo.InvariantCulture));
        }
    }
}