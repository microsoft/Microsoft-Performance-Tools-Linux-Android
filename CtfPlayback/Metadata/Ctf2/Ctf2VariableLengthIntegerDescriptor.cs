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

            return new CtfIntegerValue(DecodeLeb128(reader, this.Signed), this);
        }

        /// <summary>
        /// Decodes an unsigned or signed LEB128 value (https://en.wikipedia.org/wiki/LEB128): each byte holds 7 bits
        /// of the value, least significant group first, and its high bit is set when more bytes follow. A signed
        /// value is sign-extended from bit 6 of its last byte. Bits beyond 64 are ignored.
        /// </summary>
        internal static IntegerLiteral DecodeLeb128(IPacketReader reader, bool signed)
        {
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

            if (!signed)
            {
                return new IntegerLiteral(value);
            }

            if (shift < 64 && (current & 0x40) != 0)
            {
                value |= ulong.MaxValue << shift;
            }

            return new IntegerLiteral((long)value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Signed ? "vlint" : "vluint";
        }
    }
}
