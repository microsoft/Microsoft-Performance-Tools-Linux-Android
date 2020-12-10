// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata;
using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.FieldValues
{
    /// <summary>
    /// Represents an integer field from a CTF event
    /// </summary>
    public class CtfIntegerValue
        : CtfFieldValue
    {
        private readonly short radix;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">Field value</param>
        /// <param name="descriptor">CTF integer descriptor associated with the field</param>
        internal CtfIntegerValue(IntegerLiteral value, ICtfIntegerDescriptor descriptor)
            : base(CtfTypes.Integer)
        {
            this.Value = value;
            this.Descriptor = descriptor;

            this.MapValue = string.IsNullOrWhiteSpace(descriptor.Map) ? string.Empty : string.Intern(descriptor.Map);

            this.radix = descriptor.Base;
        }

        /// <summary>
        /// Field value
        /// </summary>
        public IntegerLiteral Value { get; }

        /// <summary>
        /// Value from the 'map' property of the Descriptor
        /// </summary>
        public string MapValue { get; }

        /// <summary>
        /// CTF integer descriptor associated with the field
        /// </summary>
        public ICtfIntegerDescriptor Descriptor { get; }

        /// <summary>
        /// Returns a string representation of the field value.
        /// Useful when text dumping the contents of an event stream.
        /// </summary>
        /// <returns>The field value as a string</returns>
        public override string GetValueAsString(GetValueAsStringOptions options = GetValueAsStringOptions.NoOptions)
        {
            if (this.Value.Signed)
            {
                return this.SignedToString();
            }

            return this.UnsignedToString();
        }

        private string SignedToString()
        {
            switch (this.radix)
            {
                case 16:
                    return string.Intern($"0x{this.Value.ValueAsLong:X}");

                default:
                    return string.Intern($"{this.Value.ValueAsLong}");
            }
        }

        private string UnsignedToString()
        {
            switch (this.radix)
            {
                case 16:
                    return string.Intern($"0x{this.Value.ValueAsUlong:X}");

                default:
                    return string.Intern($"{this.Value.ValueAsUlong}");
            }
        }
    }
}