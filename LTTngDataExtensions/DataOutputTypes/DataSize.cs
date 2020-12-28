// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace LTTngDataExtensions.DataOutputTypes
{
    public struct DataSize : IEquatable<DataSize>, IComparable<DataSize>, IComparable
    {
        const ulong BytesPerKilobyte = 1000;
        const decimal KilobytesPerByte = 1m / BytesPerKilobyte;

        const ulong BytesPerMegabyte = 1000000;
        const decimal MegabytesPerByte = 1m / BytesPerMegabyte;

        const ulong BytesPerGigabyte = 1000000000;
        const decimal GigabytesPerByte = 1m / BytesPerGigabyte;

        const ulong BytesPerTerabyte = 1000000000000;
        const decimal TerabytesPerByte = 1m / BytesPerTerabyte;

        const ulong BytesPerKibibyte = 1024;
        const decimal KibibytesPerByte = 1m / BytesPerKibibyte;

        const ulong BytesPerMebibyte = 1048576;
        const decimal MebibytesPerByte = 1m / BytesPerMebibyte;

        const ulong BytesPerGibibyte = 1073741824;
        const decimal GibibytesPerByte = 1m / BytesPerGibibyte;

        const ulong BytesPerTebibyte = 1099511627776;
        const decimal TebibytesPerByte = 1m / BytesPerTebibyte;

        public static readonly DataSize Zero = new DataSize(0);
        public static readonly DataSize MinValue = new DataSize(ulong.MinValue);
        public static readonly DataSize MaxValue = new DataSize(ulong.MaxValue);

        readonly ulong bytes;

        public DataSize(ulong bytes)
        {
            this.bytes = bytes;
        }

        internal DataSize(decimal bytes)
        {
            checked
            {
                this.bytes = (ulong) bytes;
            }
        }

        public ulong Bytes => this.bytes;

        public decimal TotalKilobytes => this.bytes * KilobytesPerByte;

        public decimal TotalMegabytes => this.bytes * MegabytesPerByte;

        public decimal TotalGigabytes => this.bytes * GigabytesPerByte;

        public decimal TotalTerabytes => this.bytes * TerabytesPerByte;

        public decimal TotalKibibytes => this.bytes * KibibytesPerByte;

        public decimal TotalMebibytes => this.bytes * MebibytesPerByte;

        public decimal TotalGibibytes => this.bytes * GibibytesPerByte;

        public decimal TotalTebibytes => this.bytes * TebibytesPerByte;

        public override int GetHashCode()
        {
            return this.bytes.GetHashCode();
        }

        public int CompareTo(object other)
        {
            // object.ReferenceEquals used here because using "==" would result in an infinite loop
            if (ReferenceEquals(other, null))
            {
                // By definition, any object compares greater than (or follows) null - MSDN
                return int.MaxValue;
            }

            if (!(other is DataSize))
            {
                throw new ArgumentException($"{nameof(other)} is not the same type as this instance.", nameof(other));
            }

            DataSize otherTyped = (DataSize) other;
            return this.CompareTo(otherTyped);
        }

        public override bool Equals(object other)
        {
            // object.ReferenceEquals used here because using "==" would result in an infinite loop
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (!(other is DataSize))
            {
                return false;
            }

            DataSize otherTyped = (DataSize) other;
            return this.bytes == otherTyped.bytes;
        }

        public bool Equals(DataSize other)
        {
            return this.bytes == other.bytes;
        }

        public int CompareTo(DataSize other)
        {
            return this.bytes.CompareTo(other.bytes);
        }

        public override string ToString()
        {
            return this.ToString(useBinaryPrefixes: true);
        }

        public string ToString(bool useBinaryPrefixes)
        {
            const string formatString = "##0.## ";

            string[] units = useBinaryPrefixes
                ? new[] {"B", "KiB", "MiB", "GiB", "TiB"}
                : new[] {"B", "KB", "MB", "GB", "TB"};
            int unitIndex = 0;
            decimal bytesDivided = this.Bytes;
            decimal divisor = useBinaryPrefixes ? BytesPerKibibyte : BytesPerKilobyte;

            while (bytesDivided >= divisor && unitIndex < units.Length - 1)
            {
                bytesDivided /= divisor;
                unitIndex++;
            }

            return bytesDivided.ToString(formatString) + units[unitIndex];
        }

        public static DataSize FromBytes(ulong bytes)
        {
            return new DataSize(bytes);
        }

        public static DataSize FromKilobytes(ulong kilobytes)
        {
            return new DataSize(kilobytes * BytesPerKilobyte);
        }

        public static DataSize FromMegabytes(ulong megabytes)
        {
            return new DataSize(megabytes * BytesPerMegabyte);
        }

        public static DataSize FromGigabytes(ulong gigabytes)
        {
            return new DataSize(gigabytes * BytesPerGigabyte);
        }

        public static DataSize FromTerabytes(ulong terabytes)
        {
            return new DataSize(terabytes * BytesPerTerabyte);
        }

        public static DataSize FromKibibytes(ulong kibibytes)
        {
            return new DataSize(kibibytes * BytesPerKibibyte);
        }

        public static DataSize FromMebibytes(ulong mebibytes)
        {
            return new DataSize(mebibytes * BytesPerMebibyte);
        }

        public static DataSize FromGibibytes(ulong gibibytes)
        {
            return new DataSize(gibibytes * BytesPerGibibyte);
        }

        public static DataSize FromTebibytes(ulong tebibytes)
        {
            return new DataSize(tebibytes * BytesPerTebibyte);
        }

        public static DataSize FromBytes(decimal bytes)
        {
            return new DataSize(bytes);
        }

        public static DataSize FromKilobytes(decimal kilobytes)
        {
            return new DataSize(kilobytes * BytesPerKilobyte);
        }

        public static DataSize FromMegabytes(decimal megabytes)
        {
            return new DataSize(megabytes * BytesPerMegabyte);
        }

        public static DataSize FromGigabytes(decimal gigabytes)
        {
            return new DataSize(gigabytes * BytesPerGigabyte);
        }

        public static DataSize FromTerabytes(decimal terabytes)
        {
            return new DataSize(terabytes * BytesPerTerabyte);
        }

        public static DataSize FromKibibytes(decimal kibibytes)
        {
            return new DataSize(kibibytes * BytesPerKibibyte);
        }

        public static DataSize FromMebibytes(decimal mebibytes)
        {
            return new DataSize(mebibytes * BytesPerMebibyte);
        }

        public static DataSize FromGibibytes(decimal gibibytes)
        {
            return new DataSize(gibibytes * BytesPerGibibyte);
        }

        public static DataSize FromTebibytes(decimal tebibytes)
        {
            return new DataSize(tebibytes * BytesPerTebibyte);
        }

        public static DataSize operator +(DataSize left, DataSize right)
        {
            ulong total;

            checked
            {
                total = left.bytes + right.bytes;
            }

            return FromBytes(total);
        }

        public static DataSize operator -(DataSize left, DataSize right)
        {
            ulong total;

            checked
            {
                total = left.bytes - right.bytes;
            }

            return FromBytes(total);
        }

        public static bool operator ==(DataSize left, DataSize right)
        {
            return left.bytes == right.bytes;
        }

        public static bool operator !=(DataSize left, DataSize right)
        {
            return left.bytes != right.bytes;
        }

        public static bool operator <(DataSize left, DataSize right)
        {
            return left.bytes < right.bytes;
        }

        public static bool operator >(DataSize left, DataSize right)
        {
            return left.bytes > right.bytes;
        }

        public static bool operator <=(DataSize left, DataSize right)
        {
            return left.bytes <= right.bytes;
        }

        public static bool operator >=(DataSize left, DataSize right)
        {
            return left.bytes >= right.bytes;
        }

        public static DataSize Min(DataSize first, DataSize second)
        {
            return FromBytes(Math.Min(first.Bytes, second.Bytes));
        }

        public static DataSize Max(DataSize first, DataSize second)
        {
            return FromBytes(Math.Max(first.Bytes, second.Bytes));
        }
    }
}
