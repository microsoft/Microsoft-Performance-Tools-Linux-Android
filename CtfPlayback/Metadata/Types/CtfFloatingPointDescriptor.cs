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

        /// exp_dig is the number of digits represented in the exponent.
        public int Exponent { get; }

        /// mant_dig is the number of digits represented in the mantissa. It is specified by the ISO C99 standard, section 5.2.4, as FLT_MANT_DIG, DBL_MANT_DIG and LDBL_MANT_DIG as defined by <float.h>.
        /// The mantissa size in bits (+1 for sign) (see CTF spec)
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

            if (byteCount == 4)
            {
                var bufferAsInt = BitConverter.ToInt32(buffer);
                var value = CreateFloat(bufferAsInt);

                return new CtfFloatValue(value, this);
            }
            else if (byteCount == 8)
            {
                var bufferAsLong = BitConverter.ToInt64(buffer);
                var value = CreateDouble(bufferAsLong);

                return new CtfDoubleValue(value, this);
            }
            else
            {
                throw new CtfPlaybackException($"Unrecognized floating_point byteCount:{byteCount}");
            }
        }

        /// Due to the way floats and doubles sometimes represent approximate but not exact values
        /// there needs to be seperate float vs double funcs. A single func returning double will also not work here

        /// <summary>
        /// Create a double from the raw encoded values
        /// </summary>
        /// <param name="rawValue">The raw value( up to 64 bits)</param>
        /// <returns></returns>
        public static double CreateDouble(long rawValue)
        {
            const int manBits = 52;
            const int expBits = 11;

            const long manShift = 1L << (manBits);
            const long manMask = manShift - 1;
            const long expMask = (1L << expBits) - 1;
            var isNegative = (rawValue & (1L << (manBits + expBits))) != 0;
            var exp = (int)((rawValue >> (manBits)) & expMask) + 1;
            var man = (rawValue & manMask);
            var offsetExponent = exp - (1 << (expBits - 1));

            double ret = man * 1.0d;
            ret /= manShift;
            // The exponents 0x000 and 0x7ff have a special meaning- see Wikipedia
            if (offsetExponent == -1023) // subnormal
            {
                var expPow = Math.Pow(2.0, -1022);
                ret *= expPow;
            }
            else if (offsetExponent == 1024 && ret != 0) // NaN - Infinity handled automatically
            {
                return double.NaN;
            }
            else
            {
                var expPow = Math.Pow(2.0, offsetExponent);
                ret += 1.0;
                ret *= expPow;
            }

            return isNegative ? -ret : ret;
        }

        /// <summary>
        /// Create a float from the raw encoded values
        /// </summary>
        /// <param name="rawValue">The raw value( up to 32 bits)</param>
        /// <returns></returns>
        public static float CreateFloat(int rawValue)
        {
            const int manBits = 23;
            const int expBits = 8;

            const int manShift = 1 << (manBits);
            const int manMask = manShift - 1;
            const int expMask = (1 << expBits) - 1;
            var isNegative = (rawValue & (1 << (manBits + expBits))) != 0;
            var exp = ((rawValue >> (manBits)) & expMask) + 1;
            var man = (rawValue & manMask);
            var offsetExponent = exp - (1 << (expBits - 1));

            float ret = man * 1.0f;
            ret /= manShift;
            // The stored exponents 00H and FFH are interpreted specially - see Wikipedia
            if (offsetExponent == -127) // subnormal
            {
                var expPow = (float)Math.Pow(2.0, -126);
                ret *= expPow;
            }
            else if (offsetExponent == 128 && ret != 0) // NaN - Infinity handled automatically
            {
                return float.NaN;
            }
            else
            {
                var expPow = (float)Math.Pow(2.0, offsetExponent);
                ret += 1.0f;
                ret *= expPow;
            }

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