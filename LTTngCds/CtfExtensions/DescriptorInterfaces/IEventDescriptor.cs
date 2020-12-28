// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Interfaces;

namespace LTTngCds.CtfExtensions.DescriptorInterfaces
{
    /// <summary>
    /// LTTng specific event information combined with CTF event information
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