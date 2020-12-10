// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.TypeInterfaces;
using CtfPlayback.Metadata.Types;
using System.Diagnostics;

namespace CtfPlayback.Metadata.NamedScopes
{
    /// <summary>
    /// Information about a single stream in the trace.
    /// </summary>
    internal class CtfStreamDescriptor
        : ICtfStreamDescriptor
    {
        private readonly ICtfTypeDescriptor header;
        private readonly ICtfTypeDescriptor context;
        private readonly ICtfTypeDescriptor eventContext;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="properties">Property bag</param>
        /// <param name="eventHeader">Event header</param>
        /// <param name="eventContext">Event context</param>
        /// <param name="packetContext">Packet context</param>
        internal CtfStreamDescriptor(
            CtfPropertyBag properties,
            CtfStructDescriptor eventHeader,
            CtfStructDescriptor eventContext,
            CtfStructDescriptor packetContext)
        {
            Debug.Assert(properties != null);
            Debug.Assert(eventHeader != null);
            Debug.Assert(packetContext != null);

            // eventContext may be null

            this.Id = properties.GetUInt("id");
            this.header = eventHeader;
            this.eventContext = eventContext;
            this.context = packetContext;
        }

        /// <summary>
        /// The stream id. Not required if there is only a single stream in the trace.
        /// </summary>
        public uint Id { get; private set; }

        /// <summary>
        /// stream.packet.context
        /// The structure defined by this descriptor exists in the packet, before any event.
        /// </summary>
        public ICtfStructDescriptor PacketContext { get { return (ICtfStructDescriptor)this.context; } }

        /// <summary>
        /// stream.event.header
        /// The structure defined by this descriptor exists in an event within a packet.
        /// </summary>
        public ICtfStructDescriptor EventHeader { get { return (ICtfStructDescriptor)this.header; } }

        /// <summary>
        /// stream.event.context
        /// The structure defined by this descriptor exists in an event within a packet, after the EventHeader.
        /// </summary>
        public ICtfStructDescriptor EventContext { get { return (ICtfStructDescriptor)this.eventContext; } }
    }
}
