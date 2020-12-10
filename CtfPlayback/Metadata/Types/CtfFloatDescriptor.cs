// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.TypeInterfaces;
using System;
using System.Diagnostics;

namespace CtfPlayback.Metadata.Types
{
    internal class CtfFloatDescriptor 
        : CtfMetadataTypeDescriptor, 
          ICtfFloatDescriptor
    {
        private readonly int align;

        internal CtfFloatDescriptor(CtfPropertyBag bag)
            : base(CtfTypes.Float)
        {
            Debug.Assert(bag != null);

            this.Exponent = bag.GetInt("exp_dig");
            this.Mantissa = bag.GetInt("mant_dig");
            this.align = bag.GetInt("align");

            this.ByteOrder = bag.GetByteOrder();
        }

        /// <inheritdoc />
        public string ByteOrder { get; }

        /// <inheritdoc />
        public int Exponent { get; }

        /// <inheritdoc />
        public int Mantissa { get; }

        /// <inheritdoc />
        public override int Align => this.align;

        /// <inheritdoc />
        public override CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null)
        {
            throw new NotImplementedException();
        }
    }
}