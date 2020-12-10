// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Interfaces;

namespace PerfCds.CtfExtensions.DescriptorInterfaces
{
    /// <summary>
    /// LTTNG specific event information combined with CTF event information. Remove for Perf??
    /// </summary>
    public interface IEventDescriptor
        : ICtfEventDescriptor
    {
        /// <summary>
        /// Event Id
        /// </summary>
        uint Id { get; }

        /// <summary>
        /// Event name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Stream id associated with the event
        /// </summary>
        int Stream { get; }
    }
}