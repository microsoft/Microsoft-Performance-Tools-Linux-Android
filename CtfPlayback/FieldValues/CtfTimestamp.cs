// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata;
using CtfPlayback.Metadata.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CtfPlayback.FieldValues
{
    // todo: optimization: we could cache the integer.map fields -> clock descriptor so that we don't have to parse each clock name

    /// <summary>
    /// Represents a CTF timestamp value
    /// </summary>
    public class CtfTimestamp
        : IComparable<CtfTimestamp>,
          IComparable,
          IEquatable<CtfTimestamp>
    {
        public const ulong NanosecondsPerSecond = 1000000000;
        public const ulong MicrosecondsPerSecond = 1000000;
        public const ulong MillisecondsPerSecond = 1000;

        // todo:this will be added to .NET Core 3.0 and .NET Standard 2.1 as DateTime.UnixEpoch. Replace this when these are adopted
        private static readonly DateTime PosixEpochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="metadataCustomization">Extensibility points</param>
        /// <param name="metadata">The active metadata relevant to this timestamp</param>
        /// <param name="integerValue">integer representation of the timestamp</param>
        public CtfTimestamp(ICtfMetadataCustomization metadataCustomization, ICtfMetadata metadata, CtfIntegerValue integerValue)
        {
            this.BaseIntegerValue = integerValue;

            string clockName = metadataCustomization.GetTimestampClockName(integerValue);

            if (string.IsNullOrWhiteSpace(clockName))
            {
                if (!string.IsNullOrWhiteSpace(integerValue.MapValue))
                {
                    throw new CtfPlaybackException($"Unable to parse integer map value as a clock: {integerValue.MapValue}");
                }

                if (metadata.Clocks?.Count == 1)
                {
                    clockName = metadata.Clocks[0].Name;
                }
                else
                {
                    Debug.Assert(false, "Add support for default clock that increments once per nanosecond: ctf spec 1.8.2 section 8");
                    throw new NotImplementedException("This library doesn't currently support a default clock.");
                }
            }

            if (!metadata.ClocksByName.TryGetValue(clockName, out var clockDescriptor))
            {
                throw new CtfPlaybackException($"Unable to retrieve clock descriptor for timestamp value: {integerValue.MapValue}");
            }

            this.ClockDescriptor = clockDescriptor;

            if (!this.BaseIntegerValue.Value.TryGetInt64(out long value))
            {
                throw new CtfPlaybackException("Unable to retrieve timestamp as long.");
            }

            this.NanosecondsFromClockBase = ConvertIntegerValueToNanoseconds(this.BaseIntegerValue);

            this.ClockName = metadataCustomization.GetTimestampClockName(integerValue);
            this.ClockOffsetFromPosixEpochInNanoseconds = ConvertTimeToNanoseconds(this.ClockDescriptor.Offset);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="metadataCustomization">Extensibility points</param>
        /// <param name="metadata">The active metadata relevant to this timestamp</param>
        /// <param name="integerValue">Integer representation of the timestamp</param>
        /// <param name="timestampValue">Timestamp value in units specified by the ClockDescriptor</param>
        public CtfTimestamp(ICtfMetadataCustomization metadataCustomization, ICtfMetadata metadata, CtfIntegerValue integerValue, long timestampValue)
            : this(metadataCustomization, metadata, integerValue)
        {
            if (timestampValue < 0)
            {
                throw new ArgumentException("Negative timestamp value is not supported.", nameof(timestampValue));
            }

            this.NanosecondsFromClockBase = ConvertTimeToNanoseconds((ulong) timestampValue);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="integerValue">Integer representation of the timestamp</param>
        /// <param name="timestampValue">Timestamp value in units specified by the ClockDescriptor</param>
        public CtfTimestamp(CtfIntegerValue integerValue, long timestampValue, ICtfClockDescriptor clockDescriptor, string clockName)
        {
            if (timestampValue < 0)
            {
                throw new ArgumentException("Negative timestamp value is not supported.", nameof(timestampValue));
            }
            this.BaseIntegerValue = integerValue;
            this.ClockDescriptor = clockDescriptor;

            if (!this.BaseIntegerValue.Value.TryGetInt64(out long value))
            {
                throw new CtfPlaybackException("Unable to retrieve timestamp as long.");
            }

            this.NanosecondsFromClockBase = ConvertTimeToNanoseconds((ulong)timestampValue);

            this.ClockName = clockName;
            this.ClockOffsetFromPosixEpochInNanoseconds = ConvertTimeToNanoseconds(this.ClockDescriptor.Offset);
        }

        /// <summary>
        /// Nanoseconds from ClockOffsetFromPosixEpoch.
        /// </summary>
        public ulong NanosecondsFromClockBase { get; }

        /// <summary>
        /// Metadata representation of the clock for this timestamp
        /// </summary>
        public ICtfClockDescriptor ClockDescriptor { get; }

        /// <summary>
        /// Base integer value of the timestamp.
        /// This value isn't always equal to the timestamp. Use <see cref="NanosecondsFromClockBase"/> instead.
        /// See CTF specification 1.8.2 section 8.
        /// </summary>
        public CtfIntegerValue BaseIntegerValue { get; }

        /// <summary>
        /// Name of the clock associated with this timestamp
        /// </summary>
        public string ClockName { get; }

        /// <summary>
        /// Ticks per second
        /// </summary>
        public ulong ClockFrequency => this.ClockDescriptor.Frequency;

        /// <summary>
        /// UUID of the clock associated with this timestamp
        /// </summary>
        public Guid ClockId => this.ClockDescriptor.Uuid;

        /// <summary>
        /// Nanoseconds from January 1st, 1970 to clock offset.
        /// </summary>
        /// <returns>Nanoseconds</returns>
        public ulong ClockOffsetFromPosixEpochInNanoseconds { get; }

        /// <summary>
        /// Nanoseconds since January 1, 1970.
        /// </summary>
        public ulong NanosecondsFromPosixEpoch => ClockOffsetFromPosixEpochInNanoseconds + NanosecondsFromClockBase;

        /// <summary>
        /// A DateTime value represented by the timestamp.
        /// </summary>
        /// <returns>Time in DateTime format</returns>
        public DateTime GetDateTime()
        {
            // Timespan is in 'ticks', which are 100s of nanoseconds
            var delta = new TimeSpan((long)(NanosecondsFromPosixEpoch / 100));
            return PosixEpochDateTime + delta;
        }

        /// <inheritdoc />
        public int CompareTo(CtfTimestamp other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            if (this.ClockId != other.ClockId)
            {
                throw new NotImplementedException(
                    "This library does not support comparisons of clocks with different UUIDs");
            }

            return this.NanosecondsFromPosixEpoch.CompareTo(other.NanosecondsFromPosixEpoch);
        }

        /// <inheritdoc />
        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return 1;
            }

            if (ReferenceEquals(this, obj))
            {
                return 0;
            }

            return obj is CtfTimestamp other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(CtfTimestamp)}");
        }

        /// <inheritdoc />
        public bool Equals(CtfTimestamp other)
        {
            return this.CompareTo(other) == 0;
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return (other is CtfTimestamp) && Equals((CtfTimestamp)other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = -303075029;
            hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(ClockId);
            hashCode = hashCode * -1521134295 + this.NanosecondsFromClockBase.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Compare CtfTimestamps, returning true if <see cref="first"/> is less than <see cref="second"/>.
        /// </summary>
        /// <param name="first">CtfTimestamp to compare</param>
        /// <param name="second">CtfTimestamp to compare</param>
        /// <returns>true if first is less than second</returns>
        public static bool operator <(CtfTimestamp first, CtfTimestamp second)
        {
            return first.CompareTo(second) < 0;
        }

        /// <summary>
        /// Compare CtfTimestamps, returning true if <see cref="first"/> is greater than than <see cref="second"/>.
        /// </summary>
        /// <param name="first">CtfTimestamp to compare</param>
        /// <param name="second">CtfTimestamp to compare</param>
        /// <returns>true if first is greater than than second</returns>
        public static bool operator >(CtfTimestamp first, CtfTimestamp second)
        {
            return first.CompareTo(second) > 0;
        }

        /// <summary>
        /// Compare CtfTimestamps, returning true if <see cref="first"/> is less than or equal to <see cref="second"/>.
        /// </summary>
        /// <param name="first">CtfTimestamp to compare</param>
        /// <param name="second">CtfTimestamp to compare</param>
        /// <returns>true if first is less than or equal to second</returns>
        public static bool operator <=(CtfTimestamp first, CtfTimestamp second)
        {
            return first.CompareTo(second) <= 0;
        }

        /// <summary>
        /// Compare CtfTimestamps, returning true if <see cref="first"/> is greater than or equal to
        /// <see cref="second"/>.
        /// </summary>
        /// <param name="first">CtfTimestamp to compare</param>
        /// <param name="second">CtfTimestamp to compare</param>
        /// <returns>true if first is greater than or equal to second</returns>
        public static bool operator >=(CtfTimestamp first, CtfTimestamp second)
        {
            return first.CompareTo(second) >= 0;
        }

        /// <summary>
        /// Compare CtfTimestamps, returning true if <see cref="first"/> is equal to <see cref="second"/>.
        /// </summary>
        /// <param name="first">CtfTimestamp to compare</param>
        /// <param name="second">CtfTimestamp to compare</param>
        /// <returns>true if first is equal to second</returns>
        public static bool operator ==(CtfTimestamp first, CtfTimestamp second)
        {
            if (first is null)
            {
                return second is null;
            }

            return first.Equals(second);
        }

        /// <summary>
        /// Compare CtfTimestamps, returning true if <see cref="first"/> is not equal to <see cref="second"/>.
        /// </summary>
        /// <param name="first">CtfTimestamp to compare</param>
        /// <param name="second">CtfTimestamp to compare</param>
        /// <returns>true if first is not equal to second</returns>
        public static bool operator !=(CtfTimestamp first, CtfTimestamp second)
        {
            if (first is null)
            {
                return !(second is null);
            }

            return !first.Equals(second);
        }

        private ulong ConvertIntegerValueToNanoseconds(CtfIntegerValue value)
        {
            if (!this.BaseIntegerValue.Value.TryGetUInt64(out var baseTime))
            {
                throw new CtfPlaybackException($"Unable to convert {nameof(CtfIntegerValue)} time into nanoseconds.");
            }

            return ConvertTimeToNanoseconds(baseTime);
        }

        private ulong ConvertTimeToNanoseconds(ulong baseTime)
        {
            if (this.ClockFrequency == NanosecondsPerSecond)
            {
                // already in nanoseconds
                return baseTime;
            }

            if (this.ClockFrequency == MicrosecondsPerSecond)
            {
                return baseTime * 1000;
            }

            if (this.ClockFrequency == MillisecondsPerSecond)
            {
                return baseTime * 1000000;
            }

            if (this.ClockFrequency == 1)
            {
                return baseTime * NanosecondsPerSecond;
            }

            throw new CtfPlaybackException($"Unsupported clock frequency: {this.ClockFrequency}");
        }
    }
}