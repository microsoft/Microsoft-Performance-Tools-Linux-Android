// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using CtfPlayback.FieldValues;
using CtfPlayback.Helpers;
using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Types
{
    internal class CtfArrayDescriptor 
        : CtfMetadataTypeDescriptor, 
          ICtfArrayDescriptor
    {
        internal CtfArrayDescriptor(ICtfTypeDescriptor type, string index)
            : base(CtfTypes.Array)
        {
            Debug.Assert(type != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(index));

            this.Type = type;
            this.Index = index;
        }

        /// <inheritdoc />
        public ICtfTypeDescriptor Type { get; }

        /// <inheritdoc />
        public string Index { get; }

        /// <inheritdoc />
        public override int Align => this.Type.Align;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.Type}[{this.Index}]";
        }

        /// <inheritdoc />
        public override CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null)
        {
            Guard.NotNull(reader, nameof(reader));

            // this will align to the underlying type when we do a Type.read

            List<CtfFieldValue> values = new List<CtfFieldValue>();

            try
            {
                IntegerLiteral integerValue;
                if (IntegerLiteralString.TryCreate(this.Index, out var integerStringIndex))
                {
                    integerValue = new IntegerLiteral(integerStringIndex);
                }
                else
                {
                    Debug.Assert(parent != null);
                    if (parent == null)
                    {
                        return null;
                    }

                    var indexField = parent.FindField(this.Index);
                    if (!(indexField is CtfIntegerValue indexValue))
                    {
                        Debug.Assert(false, "is this a valid value?");
                        return null;
                    }

                    integerValue = indexValue.Value;
                }

                if (integerValue.Signed)
                {
                    if (integerValue.ValueAsLong < 0)
                    { 
                        // we shouldn't hit this, it should be caught before now
                        throw new CtfPlaybackException("Negative array indexing is not supported.");
                    }

                    for (long x = 0; x < integerValue.ValueAsLong; x++)
                    {
                        values.Add(this.Type.Read(reader));
                    }

                    return new CtfArrayValue(values.ToArray());
                }

                for (ulong x = 0; x < integerValue.ValueAsUlong; x++)
                {
                    values.Add(this.Type.Read(reader));
                }

                return new CtfArrayValue(values.ToArray());
            }
            catch (ArgumentException)
            {
                Debug.Assert(false, "What did I miss?");
                return null;
            }
        }
    }
}