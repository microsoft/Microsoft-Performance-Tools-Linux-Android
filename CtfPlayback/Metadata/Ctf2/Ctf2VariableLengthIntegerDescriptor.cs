// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;
using CtfPlayback.Helpers;
using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.TypeInterfaces;
using CtfPlayback.Metadata.Types;

namespace CtfPlayback.Metadata.Ctf2
{
    /// <summary>
    /// CTF 2 variable-length (unsigned or signed LEB128) integer.
    /// </summary>
    internal sealed class Ctf2VariableLengthIntegerDescriptor
        : CtfMetadataTypeDescriptor,
          ICtfIntegerDescriptor
    {
        internal Ctf2VariableLengthIntegerDescriptor(bool signed, int displayBase, string map)
            : base(CtfTypes.Integer)
        {
            this.Signed = signed;
            this.Base = (short)displayBase;
            this.Map = map;
        }

        /// <inheritdoc />
        public int Size => 64;

        /// <inheritdoc />
        public bool Signed { get; }

        /// <inheritdoc />
        public string Encoding => "none";

        /// <inheritdoc />
        public short Base { get; }

        /// <inheritdoc />
        public string Map { get; }

        /// <inheritdoc />
        public override int Align => 8;

        /// <inheritdoc />
        public override CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null)
        {
            Guard.NotNull(reader, nameof(reader));

            reader.Align((uint)this.Align);

            ulong value = 0;
            int shift = 0;
            byte current;
            do
            {
                current = reader.ReadBits(8)[0];
                if (shift < 64)
                {
                    value |= (ulong)(current & 0x7f) << shift;
                }

                shift += 7;
            }
            while ((current & 0x80) != 0);

            if (!this.Signed)
            {
                return new CtfIntegerValue(new IntegerLiteral(value), this);
            }

            if (shift < 64 && (current & 0x40) != 0)
            {
                value |= ulong.MaxValue << shift;
            }

            return new CtfIntegerValue(new IntegerLiteral((long)value), this);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Signed ? "vlint" : "vluint";
        }
    }
}
