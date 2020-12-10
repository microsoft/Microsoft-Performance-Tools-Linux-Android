// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata;

namespace CtfPlayback.FieldValues
{
    /// <summary>
    /// Represents a CTF variant, which is basically a union whose value is established by an enum.
    /// </summary>
    public class CtfVariantValue
        : CtfFieldValue
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">The selected field from the variant</param>
        /// <param name="identifier">The variant tag which identifies an enum</param>
        /// <param name="tagEnum">The enum value which selected the field</param>
        public CtfVariantValue(CtfFieldValue value, string identifier, CtfEnumValue tagEnum)
            : base(CtfTypes.Variant)
        {
            this.Value = value;
            this.Identifier = identifier;
            this.TagEnum = tagEnum;
        }

        /// <summary>
        /// This is the field chosen based on the enum tag for the variant.
        /// </summary>
        public CtfFieldValue Value { get; }

        /// <summary>
        /// This is the value in the variant tag used to determine which field to
        /// use as the value of the variant.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// This is the enum that was used for the tag value.
        /// </summary>
        public CtfEnumValue TagEnum { get; }

        /// <inheritdoc />
        public override string GetValueAsString(GetValueAsStringOptions options = GetValueAsStringOptions.NoOptions)
        {
            return string.Intern($"<{Identifier}> {Value.GetValueAsString()}");
        }
    }
}