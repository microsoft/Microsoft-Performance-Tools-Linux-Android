// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using CtfPlayback.Metadata;

namespace CtfPlayback.FieldValues
{
    /// <summary>
    /// An array field. This class contains an array of other fields.
    /// </summary>
    public class CtfArrayValue
        : CtfFieldValue
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="values">array values</param>
        public CtfArrayValue(CtfFieldValue[] values)
            : base(CtfTypes.Array)
        {
            this.Value = values;
        }

        /// <summary>
        /// The array values
        /// </summary>
        public CtfFieldValue[] Value { get; }

        /// <summary>
        /// Returns a string representation of the field value.
        /// Useful when text dumping the contents of an event stream.
        /// </summary>
        /// <returns>The field value as a string</returns>
        public override string GetValueAsString(GetValueAsStringOptions options = GetValueAsStringOptions.NoOptions)
        {
            if (this.Value.Length == 0)
            {
                return "[]";
            }

            if (Value[0] is CtfIntegerValue integerValue)
            {
                if (integerValue.Descriptor.Size == 8)
                {
                    string encoding = integerValue.Descriptor.Encoding;
                    if (StringComparer.Ordinal.Equals(encoding, "ASCII") ||
                        StringComparer.Ordinal.Equals(encoding, "UTF8"))
                    {
                        if (options.HasFlag(GetValueAsStringOptions.QuotesAroundStrings))
                        {
                            return '"' + this.ReadAsString() + '"';
                        }

                        return this.ReadAsString();
                    }
                }
            }

            var sb = new StringBuilder("[ ");

            if (this.Value.Length > 0)
            {
                sb.Append("[0] = " + Value[0].GetValueAsString(options));
            }

            for (uint x = 1; x < this.Value.Length; x++)
            {
                sb.Append($", [{x}] = " + Value[x].GetValueAsString(options));
            }

            sb.Append(" ]");

            return string.Intern(sb.ToString());
        }

        /// <summary>
        /// Utility method to interpret the array contents as a string.
        /// This will only work if the array contents are a CTF integer type where the encoding property is set.
        /// </summary>
        /// <returns>The array values as a string</returns>
        public string ReadAsString()
        {
            byte[] byteArray = this.ReadAsUInt8Array();

            string result;

            if (StringComparer.Ordinal.Equals(((CtfIntegerValue)this.Value[0]).Descriptor.Encoding, "ASCII"))
            {
                result = Encoding.ASCII.GetString(byteArray);
            }
            else
            {
                result = Encoding.UTF8.GetString(byteArray);
            }

            int firstNull = result.IndexOf('\0');
            if (firstNull != -1)
            {
                result = result.Substring(0, firstNull);
            }

            return string.Intern(result.Trim('"'));
        }

        /// <summary>
        /// Utility returns the values as a signed byte array.
        /// </summary>
        /// <returns>The array values as a signed byte array</returns>
        public sbyte[] ReadAsInt8Array()
        {
            sbyte Conversion(CtfIntegerValue integerValue)
            {
                if (!integerValue.Value.TryGetInt8(out var targetValue))
                {
                    throw new CtfPlaybackException("Array cannot automatically be converted to the given type.");
                }

                return targetValue;
            }

            return ReadAsIntegerArray(Conversion);
        }

        /// <summary>
        /// Utility returns the values as a byte array.
        /// </summary>
        /// <returns>The array values as a byte array</returns>
        public byte[] ReadAsUInt8Array()
        {
            byte Conversion(CtfIntegerValue integerValue)
            {
                if (!integerValue.Value.TryGetUInt8(out var targetValue))
                {
                    throw new CtfPlaybackException("Array cannot automatically be converted to the given type.");
                }

                return targetValue;
            }

            return ReadAsIntegerArray(Conversion);
        }

        /// <summary>
        /// Utility returns the values as an int array.
        /// </summary>
        /// <returns>The array values as an int array</returns>
        public int[] ReadAsInt32Array()
        {
            int Conversion(CtfIntegerValue integerValue)
            {
                if (!integerValue.Value.TryGetInt32(out var targetValue))
                {
                    throw new CtfPlaybackException("Array cannot automatically be converted to the given type.");
                }

                return targetValue;
            }

            return ReadAsIntegerArray(Conversion);
        }

        /// <summary>
        /// Utility returns the values as a uint array.
        /// </summary>
        /// <returns>The array values as a uint array</returns>
        public uint[] ReadAsUInt32Array()
        {
            uint Conversion(CtfIntegerValue integerValue)
            {
                if (!integerValue.Value.TryGetUInt32(out var targetValue))
                {
                    throw new CtfPlaybackException("Array cannot automatically be converted to the given type.");
                }

                return targetValue;
            }

            return ReadAsIntegerArray(Conversion);
        }

        /// <summary>
        /// Utility returns the values as a long array.
        /// </summary>
        /// <returns>The array values as a long array</returns>
        public long[] ReadAsInt64Array()
        {
            long Conversion(CtfIntegerValue integerValue)
            {
                if (!integerValue.Value.TryGetInt64(out var targetValue))
                {
                    throw new CtfPlaybackException("Array cannot automatically be converted to the given type.");
                }

                return targetValue;
            }

            return ReadAsIntegerArray(Conversion);
        }

        /// <summary>
        /// Utility returns the values as a ulong array.
        /// </summary>
        /// <returns>The array values as a ulong array</returns>
        public ulong[] ReadAsUInt64Array()
        {
            ulong Conversion(CtfIntegerValue integerValue)
            {
                if (!integerValue.Value.TryGetUInt64(out var targetValue))
                {
                    throw new CtfPlaybackException("Array cannot automatically be converted to the given type.");
                }

                return targetValue;
            }

            return ReadAsIntegerArray(Conversion);
        }

        private T[] ReadAsIntegerArray<T>(Func<CtfIntegerValue, T> conversion)
        {
            var targetArray = new T[this.Value.Length];

            for (int x = 0; x < this.Value.Length; x++)
            {
                var comValue = this.Value[x];

                if (!(comValue is CtfIntegerValue comInteger))
                {
                    throw new CtfPlaybackException("Array entry is not of an integer type.");
                }

                targetArray[x] = conversion(comInteger);
            }

            return targetArray;
        }
    }
}