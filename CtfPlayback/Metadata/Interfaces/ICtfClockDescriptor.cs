// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace CtfPlayback.Metadata.Interfaces
{
    /// <summary>
    /// Describes a clock from the CTF metadata.
    /// </summary>
    public interface ICtfClockDescriptor
    {
        /// <summary>
        /// Mandatory clock identifier.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Initial frequency of the clock in Hz.
        /// Default value is 1000000000 (1ns).
        /// </summary>
        ulong Frequency { get; }

        /// <summary>
        /// Describes uncertainty in the clock measurements.
        /// Unit is (1/freq) - nanoseconds in the default case.
        /// </summary>
        ulong Precision { get; }

        /// <summary>
        /// Offset from the POSIX.1 Epoch, 1970-01-01 00:00:00 +0000 (UTC).
        /// Unit: (1/freq) - nanoseconds in the default case.
        /// Defaults to zero.
        /// </summary>
        ulong Offset { get; }

        /// <summary>
        /// This is used to correlate different traces that use the same clock.
        /// </summary>
        Guid Uuid { get; }

        /// <summary>
        /// When true, the clock may be synchronized with any other clocks.
        /// When false, the clock may only be synchronized to clocks with the same UUID.
        /// </summary>
        bool Absolute { get; }
    }
}