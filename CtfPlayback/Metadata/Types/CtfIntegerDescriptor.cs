// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CtfPlayback.FieldValues;
using CtfPlayback.Helpers;
using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Types
{
    internal class CtfIntegerDescriptor 
        : CtfMetadataTypeDescriptor,
          ICtfIntegerDescriptor,
          IEquatable<CtfIntegerDescriptor>
    {
        private int align = 8;

        internal CtfIntegerDescriptor(CtfPropertyBag bag)
            : base(CtfTypes.Integer)
        {
            Debug.Assert(bag != null);

            // size is the only required field for an integer
            this.Size = bag.GetShort("size");
            if (this.Size <= 0)
            {
                throw new ArgumentException("An integer size must be greater than zero.");
            }

            this.SetAlignment(bag);
            this.SetSigned(bag);
            this.SetEncoding(bag);
            this.SetBase(bag);

            this.ByteOrder = bag.GetByteOrder();

            // there is no default for "map", as it is not defined in specification 1.82, but
            // examples show it referencing a clock name
            this.Map = bag.GetString("map");
        }

        /// <inheritdoc />
        public int Size { get; private set; }

        /// <inheritdoc />
        public bool Signed { get; private set; }

        /// <inheritdoc />
        public string Encoding { get; private set; }

        /// <inheritdoc />
        public short Base { get; private set; }

        /// <inheritdoc />
        public string Map { get; private set; }

        /// <inheritdoc />
        public override int Align => this.align;

        internal string ByteOrder { get; private set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return (this.Signed ? "int" : "uint") + this.Size.ToString();
        }

        /// <inheritdoc />
        public override CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null)
        {
            Guard.NotNull(reader, nameof(reader));

            reader.Align((uint)this.Align);
            byte[] buffer = reader.ReadBits((uint)this.Size);
            if(buffer == null)
            {
                throw new CtfPlaybackException("IPacketReader.ReadBits returned null while reading an integer value.");
            }

            int byteCount = buffer.Length;
            return this.Read(buffer, byteCount);
        }

        internal CtfFieldValue Read(byte[] buffer, int byteCount)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(byteCount > 0);

            if (this.Size > 64)
            {
                throw new NotImplementedException("Integers greater than 64-bits are not supported.");
            }

            // todo:check for big endian values - check the byte order
            // if byte order is not set, or if the value is "native", then endianness is determined by the trace descriptor
            // if the byte order is "be" or "network", then it is big endian
            // if the byte order is "le", then it is little endian

            IntegerLiteral value;

            if (this.Signed)
            {
                value = ReadSignedLittleEndianValue(buffer, byteCount);
            }
            else
            {
                value = ReadUnsignedLittleEndianValue(buffer, byteCount);
            }

            return new CtfIntegerValue(value, this);
        }

        /// <inheritdoc />
        public bool Equals(CtfIntegerDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.align == other.align && 
                   this.Size == other.Size && 
                   this.Signed == other.Signed && 
                   string.Equals(this.Encoding, other.Encoding) && 
                   this.Base == other.Base && 
                   string.Equals(this.Map, other.Map) && 
                   string.Equals(this.ByteOrder, other.ByteOrder);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((CtfIntegerDescriptor) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.align;
                hashCode = (hashCode * 397) ^ this.Size;
                hashCode = (hashCode * 397) ^ this.Signed.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.Encoding != null ? this.Encoding.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.Base.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.Map != null ? this.Map.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.ByteOrder != null ? this.ByteOrder.GetHashCode() : 0);
                return hashCode;
            }
        }

        private IntegerLiteral ReadSignedLittleEndianValue(byte[] buffer, int byteCount)
        {
            long value = 0;

            for (int x = byteCount - 1; x >= 0; x--)
            {
                value = value << 8;
                value |= buffer[x];
            }

            int signedMask = 1 << (this.Size - 1);
            if ((value & signedMask) != 0)
            {
                // extend the high order signed bit
                long mask = ~(signedMask - 1);
                value = value | mask;
            }

            return new IntegerLiteral(value);
        }

        private IntegerLiteral ReadUnsignedLittleEndianValue(byte[] buffer, int byteCount)
        {
            ulong value = 0;

            for (int x = byteCount - 1; x >= 0; x--)
            {
                value = value << 8;
                value |= buffer[x];
            }

            return new IntegerLiteral(value);
        }

        private IntegerLiteral ReadSignedBigEndianValue(byte[] buffer, int byteCount)
        {
            long value = 0;

            for (int x = 0; x < byteCount; x++)
            {
                value = value << 8;
                value |= buffer[x];
            }

            return new IntegerLiteral(value);
        }

        private IntegerLiteral ReadUnsignedBigEndianValue(byte[] buffer, int byteCount)
        {
            ulong value = 0;

            for (int x = 0; x < byteCount; x++)
            {
                value = value << 8;
                value |= buffer[x];
            }

            return new IntegerLiteral(value);
        }

        private void SetAlignment(CtfPropertyBag bag)
        {
            var alignment = bag.GetShortOrNull("align");
            if (alignment.HasValue)
            {
                this.align = alignment.Value;
                return;
            }

            // if alignment is not specifically set, then integers with a size which is multiple
            // of 8-bits will be 8-bit aligned, all others will be single byte aligned.
            // spec: 1.8.2, 4.1.2
            if ((bag.GetShort("size") % 8) == 0)
            {
                this.align = 8;
            }
            else
            {
                this.align = 1;
            }
        }

        private void SetSigned(CtfPropertyBag bag)
        {
            if (!bag.TryGetBoolean("signed", out bool signed))
            {
                // integers default to unsigned according to specification 1.82 section 4.1.5
                this.Signed = false;
                return;
            }

            this.Signed = signed;
        }

        private void SetEncoding(CtfPropertyBag bag)
        {
            // integers default to no encoding to specification 1.82 section 4.1.5
            this.Encoding = bag.GetString("encoding") ?? "none";
        }

        private void SetBase(CtfPropertyBag bag)
        {
            // integers default to decimal to specification 1.82 section 4.1.5
            this.Base = bag.GetShortOrNull("base") ?? 10;
        }
    }
}