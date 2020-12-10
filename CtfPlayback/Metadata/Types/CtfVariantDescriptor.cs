// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CtfPlayback.FieldValues;
using CtfPlayback.Helpers;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Types
{
    internal class CtfVariantDescriptor 
        : CtfMetadataTypeDescriptor, 
          ICtfVariantDescriptor
    {
        internal CtfVariantDescriptor(string switchName, IReadOnlyList<ICtfFieldDescriptor> union)
            : base(CtfTypes.Variant)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(switchName));
            Debug.Assert(union != null);

            this.Switch = switchName;
            this.Union = union;
        }

        /// <inheritdoc />
        public string Switch { get; }

        /// <inheritdoc />
        public IReadOnlyList<ICtfFieldDescriptor> Union { get; }

        /// <inheritdoc />
        public ICtfFieldDescriptor GetVariant(string name)
        {
            Guard.NotNullOrWhiteSpace(name, nameof(name));

            return this.Union.SingleOrDefault(f => f.Name == name);
        }

        /// <summary>
        /// The alignment of a variant cannot be predetermined. It is based on the field that is actually chosen for
        /// use from the variant.
        /// </summary>
        public override int Align => 1;

        /// <inheritdoc />
        public override CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null)
        {
            Guard.NotNull(reader, nameof(reader));

            // todo:handle dynamic scoped tags

            if (parent == null)
            {
                throw new CtfPlaybackException("No parent specified for variant. Unable to find variant tag for reading.");
            }

            var tagFieldValue = parent.FindField(this.Switch);
            if (tagFieldValue == null)
            {
                throw new CtfPlaybackException($"Unable to find variant tag for reading: {this.Switch}.");
            }

            if (!(tagFieldValue is CtfEnumValue tagEnum))
            {
                throw new CtfPlaybackException($"The tag field is not an enumeration value: {this.Switch}.");
            }

            var variantTypeToRead = GetVariant(tagEnum.Value);
            if (variantTypeToRead == null)
            {
                throw new CtfPlaybackException($"The enumeration value did not match a field in the variant: {tagEnum.Value}.");
            }

            var variantValue = variantTypeToRead.Read(reader, parent);

            return new CtfVariantValue(variantValue, tagEnum.Value, tagEnum);
        }
    }
}