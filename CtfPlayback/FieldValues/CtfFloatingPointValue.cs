// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using CtfPlayback.Metadata;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.FieldValues
{
    /// <summary>
    /// Represents a floating point field from a CTF event
    /// </summary>
    public class CtfFloatingPointValue
        : CtfFieldValue
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">Floating point value</param>
        public CtfFloatingPointValue(double value, ICtfFloatingPointDescriptor descriptor)
            : base(CtfTypes.FloatingPoint)
        {
            this.Value = value;
            this.Descriptor = descriptor;
        }

        /// <summary>
        /// Field value
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// CTF floating point descriptor associated with the field
        /// </summary>
        public ICtfFloatingPointDescriptor Descriptor { get; }

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