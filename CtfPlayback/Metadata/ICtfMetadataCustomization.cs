// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.Metadata
{
    /// <summary>
    /// Extensibility points for dealing with trace metadata.
    /// </summary>
    public interface ICtfMetadataCustomization
    {
        /// <summary>
        /// The trace metadata.
        /// </summary>
        ICtfMetadata Metadata { get; }

        /// <summary>
        /// Retrieve a clock name from an integer type.
        /// It's not clear to me that the CTF specification mandates this clock reference format.
        /// There is an example in the specification: clock.<clock_name>.value in the "map" field.
        /// where <clock_name> is the name of the clock
        /// e.g. "clock.monotonic.value"
        /// </summary>
        /// <param name="timestampField">The integer class that should have a clock name</param>
        /// <returns>The clock name</returns>
        string GetTimestampClockName(CtfIntegerValue timestampField);
    }
}