// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;
using CtfPlayback.Helpers;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.TypeInterfaces;
using System;
using System.Diagnostics;

namespace CtfPlayback.Metadata.Types
{
    internal class CtfFloatingPointDescriptor 
        : CtfMetadataTypeDescriptor, 
          ICtfFloatingPointDescriptor
    {
        private readonly int align;

        internal CtfFloatingPointDescriptor(CtfPropertyBag bag)
            : base(CtfTypes.FloatingPoint)
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
        /// exp_dig is the number of digits represented in the exponent.
        public int Exponent { get; }

        /// <inheritdoc />
        /// mant_dig is the number of digits represented in the mantissa. It is specified by the ISO C99 standard, section 5.2.4, as FLT_MANT_DIG, DBL_MANT_DIG and LDBL_MANT_DIG as defined by <float.h>.
        /// mant_dig is one bit more than its actual size in bits (leading 1 is not needed)
        public int Mantissa { get; }

        /// <inheritdoc />
        public override int Align => this.align;

        /// <inheritdoc />
        public override CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null)
        {
            Guard.NotNull(reader, nameof(reader));

            reader.Align((uint)this.Align);

            byte[] buffer = null;
            if ((Exponent + Mantissa) == 32)
            {
                buffer = reader.ReadBits(32);
            }
            else if ((Exponent + Mantissa) == 64)
            {
                buffer = reader.ReadBits(64);
            }
            if (buffer == null)
            {
                throw new CtfPlaybackException("IPacketReader.ReadBits returned null while reading an floating_point value.");
            }

            int byteCount = buffer.Length;
            return this.Read(buffer, byteCount);
        }

        internal CtfFieldValue Read(byte[] buffer, int byteCount)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(byteCount > 0);
            Debug.Assert(byteCount <= 8);  // Up to 64-bits

            long bufferAsLong;
            if (byteCount == 4)
            {
                bufferAsLong = BitConverter.ToInt32(buffer);
            }
            else if (byteCount == 8)
            {
                bufferAsLong = BitConverter.ToInt64(buffer);
            }
            else
            {
                throw new CtfPlaybackException($"Unrecognized floating_point byteCount:{byteCount}");
            }

            var value = CreateDouble(bufferAsLong, Mantissa - 1, Exponent);

            return new CtfFloatingPointValue(value, this);
        }

        /// <summary>
        /// Create a float from the raw encoded values
        /// </summary>
        /// <param name="rawValue">The raw value( up to 64 bits)</param>
        /// <param name=""></param>
        /// <param name="manBits">Number of bits in the mantissa</param>
        /// <param name=""></param>
        /// <param name="expBits">Number of bits in the exponent</param>
        /// <returns></returns>
        private static double CreateDouble(long rawValue, int manBits, int expBits)
        {
            double ret = double.NaN;

            var manShift = 1L << (manBits);
            var manMask = manShift - 1;
            var expMask = (1L << expBits) - 1;
            var isNegative = (rawValue & (1L << (manBits + expBits))) != 0;
            var exp = (int)((rawValue >> (manBits)) & expMask) + 1;
            var man = (rawValue & manMask);
            var offsetExponent = exp - (1 << (expBits - 1));
            double expPow = Math.Pow(2.0, offsetExponent);
            ret = man * 1.0f;
            ret /= manShift;
            ret += 1.0;
            ret *= expPow;

            return isNegative ? -ret : ret;
        }

        /// <inheritdoc />
        public bool Equals(CtfFloatingPointDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (Align != other.Align)
            {
                return false;
            }
            if (!ByteOrder.Equals(other.ByteOrder))
            {
                return false;
            }
            if (Exponent != other.Exponent)
            {
                return false;
            }
            return (Mantissa == other.Mantissa);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((CtfFloatingPointDescriptor)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + (Align ^ (Align >> 32));
            // don't evaluate object but string
            result = prime * result + ByteOrder.GetHashCode();
            result = prime * result + Exponent;
            result = prime * result + Mantissa;
            return result;
        }
    }
}