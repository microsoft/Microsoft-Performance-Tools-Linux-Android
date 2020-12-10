// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace CtfPlayback.Metadata.Interfaces
{
    /// <summary>
    /// Represents the metadata from a CTF trace
    /// </summary>
    public interface ICtfMetadata
    {
        /// <summary>
        /// The trace descriptor
        /// </summary>
        ICtfTraceDescriptor TraceDescriptor { get; }

        /// <summary>
        /// The environment descriptor
        /// </summary>
        ICtfEnvironmentDescriptor EnvironmentDescriptor { get; }

        /// <summary>
        /// All of the clock descriptors
        /// </summary>
        IReadOnlyList<ICtfClockDescriptor> Clocks { get; }

        /// <summary>
        /// Clock descriptors accessible by name
        /// </summary>
        IReadOnlyDictionary<string, ICtfClockDescriptor> ClocksByName { get; }

        /// <summary>
        /// All of the stream descriptors
        /// </summary>
        IReadOnlyList<ICtfStreamDescriptor> Streams { get; }

        /// <summary>
        /// All of the event descriptors
        /// </summary>
        IReadOnlyList<ICtfEventDescriptor> Events { get; }
    }
}