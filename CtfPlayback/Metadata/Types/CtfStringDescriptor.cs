// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CtfPlayback.FieldValues;
using CtfPlayback.Helpers;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Types
{
    internal class CtfStringDescriptor 
        : CtfMetadataTypeDescriptor, 
          ICtfStringDescriptor
    {
        /// <summary>
        /// Checks for an encoding value in the property bag and determines if it is valid.
        /// </summary>
        /// <param name="bag">Property bag</param>
        /// <returns>true if valid</returns>
        internal static bool IsValidEncoding(CtfPropertyBag bag)
        {
            return GetEncoding(bag) != EncodingTypes.Invalid;
        }

        internal CtfStringDescriptor(CtfPropertyBag bag)
            : base(CtfTypes.String)
        {
            Debug.Assert(bag != null);

            this.SetEncoding(bag);
        }

        /// <inheritdoc />
        public EncodingTypes Encoding { get; private set; }

        /// <inheritdoc />
        /// <remarks>
        /// According to specification 1.8.2 section 4.2.5, strings are always byte-aligned
        /// </remarks>
        public override int Align => 8;

        /// <inheritdoc />
        public override CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null)
        {
            Guard.NotNull(reader, nameof(reader));

            reader.Align((uint)this.Align);

            byte[] value = reader.ReadString();

            if (this.Encoding == EncodingTypes.Ascii)
            {
                return new CtfStringValue(System.Text.Encoding.ASCII.GetString(value, 0, value.Length - 1));
            }

            return new CtfStringValue(System.Text.Encoding.UTF8.GetString(value, 0, value.Length - 1));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "string";
        }

        private void SetEncoding(CtfPropertyBag bag)
        {
            this.Encoding = GetEncoding(bag);
        }

        private static EncodingTypes GetEncoding(CtfPropertyBag bag)
        {
            Debug.Assert(bag != null);

            if (!bag.TryGetString("encoding", out string encoding))
            {
                // strings default to UTF-8 encoding to specification 1.82 section 4.2.5
                return EncodingTypes.Utf8;
            }

            if (StringComparer.InvariantCulture.Equals(encoding, "UTF8"))
            {
                return EncodingTypes.Utf8;
            }

            if (StringComparer.InvariantCulture.Equals(encoding, "ASCII"))
            {
                return EncodingTypes.Ascii;
            }

            return EncodingTypes.Invalid;
        }
    }
}