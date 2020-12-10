// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Interfaces
{
    /// <summary>
    /// A CTF stream descriptor
    /// </summary>
    public interface ICtfStreamDescriptor
    {
        /// <summary>
        /// Stream id is optional if there is only one stream in the trace.
        /// </summary>
        uint Id { get; }

        /// <summary>
        /// stream.packet.context
        /// The structure defined by this descriptor exists in the packet, before any event.
        /// </summary>
        ICtfStructDescriptor PacketContext { get; }

        /// <summary>
        /// stream.event.header
        /// The structure defined by this descriptor exists in an event within a packet.
        /// </summary>
        ICtfStructDescriptor EventHeader { get; }

        /// <summary>
        /// stream.event.context
        /// The structure defined by this descriptor exists in an event within a packet, after the EventHeader.
        /// </summary>
        ICtfStructDescriptor EventContext { get; }
    }
}