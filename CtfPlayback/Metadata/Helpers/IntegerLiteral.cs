// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CtfPlayback.Metadata.InternalHelpers;

namespace CtfPlayback.Metadata.Helpers
{
    /// <summary>
    /// This class is used to wrap integer literal values with a set of helper functions.
    /// </summary>
    public class IntegerLiteral
    {
        internal static readonly ushort MaximumSupportedBitCount = 64;

        private readonly IntegerLiteralString originalString;

        private long valueAsLong;
        private ulong valueAsUlong;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="integerAsString">integer value as a string</param>
        /// <returns>An IntegerLiteral instance</returns>
        internal static IntegerLiteral CreateIntegerLiteral(string integerAsString)
        {
            var integerString = new IntegerLiteralString(integerAsString);
            return new IntegerLiteral(integerString);
        }

        internal IntegerLiteral(IntegerLiteralString integerLiteralString)
        {
            Debug.Assert(integerLiteralString != null);

            this.originalString = integerLiteralString;
            this.ProcessOriginalString();
        }

        internal IntegerLiteral(long value)
        {
            this.Assign(value);
        }

        internal IntegerLiteral(ulong value)
        {
            this.Assign(value);
        }

        internal IntegerLiteral(IntegerLiteral other)
        {
            Debug.Assert(other != null);

            if (other.Signed)
            {
                this.Assign(other.ValueAsLong);
            }
            else
            {
                this.Assign(other.ValueAsUlong);
            }
        }

        /// <summary>
        /// True if the value is signed.
        /// </summary>
        public bool Signed { get; private set; }

        /// <summary>
        /// The number of bits required to represent the value.
        /// </summary>
        internal ushort RequiredBitCount { get; private set; }

        /// <summary>
        /// Add the value to the IntegerLiteral value.
        /// </summary>
        /// <param name="integerLiteral">IntegerLiteral</param>
        /// <param name="value">Value to add</param>
        /// <returns>The addition result</returns>
        public static long operator +(IntegerLiteral integerLiteral, long value)
        {
            if (integerLiteral.Signed)
            {
                return integerLiteral.ValueAsLong + value;
            }
            else
            {
                throw new ArgumentException(
                    $"Cannot add a signed value to an unsigned {nameof(IntegerLiteral)}.",
                    nameof(value));
            }
        }

        /// <summary>
        /// Add the value to the IntegerLiteral value.
        /// </summary>
        /// <param name="integerLiteral">IntegerLiteral</param>
        /// <param name="value">Value to add</param>
        /// <returns>The addition result</returns>
        public static ulong operator +(IntegerLiteral integerLiteral, ulong value)
        {
            if (!integerLiteral.Signed)
            {
                return integerLiteral.ValueAsUlong + value;
            }
            else
            {
                throw new ArgumentException(
                    $"Cannot add an unsigned value to an signed {nameof(IntegerLiteral)}.",
                    nameof(value));
            }
        }

        /// <summary>
        /// Try to return the IntegerValue as a signed 8-bit value.
        /// </summary>
        /// <param name="value">The requested value</param>
        /// <returns>True if successful</returns>
        public bool TryGetInt8(out sbyte value)
        {
            value = sbyte.MinValue;

            if (this.Signed)
            {
                if (this.RequiredBitCount > 8)
                {
                    return false;
                }

                value = (sbyte) ValueAsLong;
                return true;
            }

            // signed types require one extra bit for the sign, so it requires one more bit than its
            // unsigned counterpart
            if (this.RequiredBitCount > 7)
            {
                return false;
            }

            value = (sbyte) ValueAsUlong;
            return true;
        }

        /// <summary>
        /// Try to return the IntegerValue as an unsigned 8-bit value.
        /// </summary>
        /// <param name="value">The requested value</param>
        /// <returns>True if successful</returns>
        public bool TryGetUInt8(out byte value)
        {
            value = byte.MinValue;

            if (this.Signed)
            {
                // signed types requires one extra bit for the sign, so the unsigned may use one extra bit than its
                // signed counterpart
                if (this.RequiredBitCount > 9)
                {
                    return false;
                }

                value = (byte) ValueAsLong;
                return true;
            }

            if (this.RequiredBitCount > 8)
            {
                return false;
            }

            value = (byte) ValueAsUlong;
            return true;
        }

        /// <summary>
        /// Try to return the IntegerValue as a signed 16-bit value.
        /// </summary>
        /// <param name="value">The requested value</param>
        /// <returns>True if successful</returns>
        public bool TryGetInt16(out short value)
        {
            value = short.MinValue;

            if (this.Signed)
            {
                if (this.RequiredBitCount > 16)
                {
                    return false;
                }

                value = (short) ValueAsLong;
                return true;
            }

            // signed types require one extra bit for the sign, so it requires one more bit than its
            // unsigned counterpart
            if (this.RequiredBitCount > 15)
            {
                return false;
            }

            value = (short) ValueAsUlong;
            return true;
        }

        /// <summary>
        /// Try to return the IntegerValue as an unsigned 16-bit value.
        /// </summary>
        /// <param name="value">The requested value</param>
        /// <returns>True if successful</returns>
        public bool TryGetUInt16(out ushort value)
        {
            value = ushort.MinValue;

            if (this.Signed)
            {
                // signed types requires one extra bit for the sign, so the unsigned may use one extra bit than its
                // signed counterpart
                if (this.RequiredBitCount > 15)
                {
                    return false;
                }

                value = (ushort) ValueAsLong;
                return true;
            }

            if (this.RequiredBitCount > 16)
            {
                return false;
            }

            value = (ushort) ValueAsUlong;
            return true;
        }

        /// <summary>
        /// Try to return the IntegerValue as a signed 32-bit value.
        /// </summary>
        /// <param name="value">The requested value</param>
        /// <returns>True if successful</returns>
        public bool TryGetInt32(out int value)
        {
            value = int.MinValue;

            if (this.Signed)
            {
                if (this.RequiredBitCount > 32)
                {
                    return false;
                }

                value = (int) ValueAsLong;
                return true;
            }

            // signed types require one extra bit for the sign, so it requires one more bit than its
            // unsigned counterpart
            if (this.RequiredBitCount > 31)
            {
                return false;
            }

            value = (int) ValueAsUlong;
            return true;
        }

        /// <summary>
        /// Try to return the IntegerValue as an unsigned 32-bit value.
        /// </summary>
        /// <param name="value">The requested value</param>
        /// <returns>True if successful</returns>
        public bool TryGetUInt32(out uint value)
        {
            value = uint.MinValue;

            if (this.Signed)
            {
                // signed types requires one extra bit for the sign, so the unsigned may use one extra bit than its
                // signed counterpart
                if (this.RequiredBitCount > 33)
                {
                    return false;
                }

                value = (uint) ValueAsLong;
                return true;
            }

            if (this.RequiredBitCount > 32)
            {
                return false;
            }

            value = (uint) ValueAsUlong;
            return true;
        }

        /// <summary>
        /// Try to return the IntegerValue as a signed 64-bit value.
        /// </summary>
        /// <param name="value">The requested value</param>
        /// <returns>True if successful</returns>
        public bool TryGetInt64(out long value)
        {
            value = long.MinValue;

            if (this.Signed)
            {
                if (this.RequiredBitCount > 64)
                {
                    return false;
                }

                value = (long) ValueAsLong;
                return true;
            }

            // signed types require one extra bit for the sign, so it requires one more bit than its
            // unsigned counterpart
            if (this.RequiredBitCount > 63)
            {
                return false;
            }

            value = (long) ValueAsUlong;
            return true;
        }

        /// <summary>
        /// Try to return the IntegerValue as an unsigned 64-bit value.
        /// </summary>
        /// <param name="value">The requested value</param>
        /// <returns>True if successful</returns>
        public bool TryGetUInt64(out ulong value)
        {
            value = ulong.MinValue;

            if (this.Signed)
            {
                // signed types requires one extra bit for the sign, so the unsigned may use one extra bit than its
                // signed counterpart
                if (this.RequiredBitCount > 65)
                {
                    return false;
                }

                value = (ulong) ValueAsLong;
                return true;
            }

            if (this.RequiredBitCount > 64)
            {
                return false;
            }

            value = (ulong) ValueAsUlong;
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (this.Signed)
            {
                return this.ValueAsLong.ToString();
            }

            return this.ValueAsUlong.ToString();
        }

        internal long ValueAsLong
        {
            get
            {
                if (!this.Signed)
                {
                    throw new NotSupportedException(
                        $"An unsigned {nameof(IntegerLiteral)} cannot access {nameof(this.ValueAsLong)}. " +
                        $"Use {nameof(this.ValueAsUlong)} instead.");
                }

                return this.valueAsLong;
            }
            private set { this.valueAsLong = value; }
        }

        internal ulong ValueAsUlong
        {
            get
            {
                if (this.Signed)
                {
                    throw new NotSupportedException(
                        $"An signed {nameof(IntegerLiteral)} cannot access {nameof(this.ValueAsUlong)}. " +
                        $"Use {nameof(this.ValueAsLong)} instead.");
                }

                return this.valueAsUlong;
            }
            private set { this.valueAsUlong = value; }
        }

        internal bool ConvertSignedValue(bool signed)
        {
            if (this.Signed == signed)
            {
                return true;
            }

            if (signed)
            {
                return this.ConvertToSigned();
            }

            return this.ConvertToUnsigned();
        }

        internal bool ConvertToSigned(bool allowNegativeResult = false)
        {
            if (this.Signed)
            {
                // already signed
                return true;
            }

            if (this.RequiredBitCount == 64 && !allowNegativeResult)
            {
                // the value already requires 64 bits. making it signed would require one more bit, which we don't
                // currently support
                return false;
            }

            this.ValueAsLong = (long)this.ValueAsUlong;
            this.ValueAsUlong = 0;
            this.Signed = true;

            if (!allowNegativeResult)
            {
                // For the signed bit
                this.RequiredBitCount++;
            }

            return true;
        }

        internal bool ConvertToUnsigned(bool forceNegativeValues = false)
        {
            if (!this.Signed)
            {
                // already unsigned
                return true;
            }

            if (this.valueAsLong < 0 && !forceNegativeValues)
            {
                // the value is negative, it cannot be made unsigned
                return false;
            }

            this.ValueAsUlong = (ulong)this.ValueAsLong;
            this.ValueAsLong = 0;
            this.Signed = false;

            // For the signed bit
            this.RequiredBitCount--;
            return true;
        }

        internal void UpdateValue(long newValue)
        {
            if (!this.Signed)
            {
                throw new ArgumentException(
                    $"Cannot assign a signed value to an unsigned {nameof(IntegerLiteral)}",
                    nameof(newValue));
            }

            this.valueAsLong = newValue;
            this.RequiredBitCount = this.DetermineRequiredBitCount();
        }

        internal void UpdateValue(ulong newValue)
        {
            if (this.Signed)
            {
                throw new ArgumentException(
                    $"Cannot assign an unsigned value to an signed {nameof(IntegerLiteral)}",
                    nameof(newValue));
            }

            this.valueAsUlong = newValue;
            this.RequiredBitCount = this.DetermineRequiredBitCount();
        }

        private void Assign(long value)
        {
            this.Signed = true;
            this.valueAsLong = value;
            this.DetermineRequiredBitCount();
        }

        private void Assign(ulong value)
        {
            this.Signed = false;
            this.valueAsUlong = value;
            this.DetermineRequiredBitCount();
        }

        private void ProcessOriginalString()
        {
            this.Signed = this.IsSigned(this.originalString.NumberSuffix);
            this.DetermineValue();
            this.RequiredBitCount = this.DetermineRequiredBitCount();
        }

        private void DetermineValue()
        {
            if (this.Signed)
            {
                try
                {
                    this.ValueAsLong = Convert.ToInt64(this.originalString.PlainNumber, this.originalString.NumberBase);
                }
                catch (FormatException)
                {
                    Debug.Assert(false, "Is the regular expression must be wrong if this is hit.");
                }
                catch (OverflowException)
                {
                    throw new NotSupportedException("Literal integers are limited to 64-bits.");
                }
            }
            else
            {
                try
                {
                    this.ValueAsUlong = Convert.ToUInt64(this.originalString.PlainNumber, this.originalString.NumberBase);
                }
                catch (FormatException)
                {
                    Debug.Assert(false, "Is the regular expression must be wrong if this is hit.");
                }
                catch (OverflowException)
                {
                    throw new NotSupportedException("Literal integers are limited to 64-bits.");
                }
            }
        }

        private ushort DetermineRequiredBitCount()
        {
            if (this.Signed)
            {
                return DetermineRequiredBitCount(this.ValueAsLong);
            }

            return DetermineRequiredBitCount(this.ValueAsUlong);
        }

        // For now, we only support integer literals that fit into a 64-bit value.
        private static ushort DetermineRequiredBitCount(long value)
        {
            ushort requiredBitCount = HighestUsedBit(value);

            // the last bit in a signed value is used to represent sign, so we need an extra bit
            requiredBitCount++;

            return requiredBitCount;
        }

        private static ushort DetermineRequiredBitCount(ulong value)
        {
            return HighestUsedBit(value);
        }

        private static ushort HighestUsedBit(long value)
        {
            // for our purposes, we always want at least one bit
            if (value == 0)
            {
                return 1;
            }

            ushort highestUsedBit = 0;

            // a negative number would always use the most significant bit
            value = Math.Abs(value);

            do
            {
                highestUsedBit++;
                value = value >> 1;
            } while (value != 0);

            return highestUsedBit;
        }

        private static ushort HighestUsedBit(ulong value)
        {
            // for our purposes, we always want at least one bit
            if (value == 0)
            {
                return 1;
            }

            ushort highestUsedBit = 0;

            do
            {
                highestUsedBit++;
                value = value >> 1;
            } while (value != 0);

            return highestUsedBit;
        }

        private string ParseIntegerSuffix(Group suffixGroup)
        {
            if (!suffixGroup.Success)
            {
                return string.Empty;
            }

            return suffixGroup.Value;
        }

        private bool IsSigned(string integerSuffix)
        {
            if (integerSuffix.Contains("u"))
            {
                return false;
            }

            return true;
        }
    }
}