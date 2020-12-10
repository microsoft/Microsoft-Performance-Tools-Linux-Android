// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using CtfPlayback.EventStreams.Interfaces;
using CtfPlayback.FieldValues;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.EventStreams
{
    internal class CtfEvent
        : ICtfEvent
    {
        private readonly PacketReader packetReader;
        private readonly ICtfPacket owningPacket;

        public CtfEvent(PacketReader packetReader, ICtfPacket owningPacket)
        {
            this.packetReader = packetReader;
            this.owningPacket = owningPacket;
        }

        public ulong ByteOffsetWithinPacket { get; private set; }

        public CtfTimestamp Timestamp { get; private set; }

        public CtfFieldValue StreamDefinedEventHeader { get; private set; }

        public CtfFieldValue StreamDefinedEventContext { get; private set; }

        public IReadOnlyList<CtfFieldValue> Properties { get; set; }

        public CtfFieldValue Context { get; set; }

        public CtfFieldValue Payload { get; set; }

        public uint PayloadBitCount { get; set; }

        public uint DiscardedEvents { get; set; }

        public ICtfEventDescriptor EventDescriptor { get; private set; }

        public void ReadEventMetadata()
        {
            this.ByteOffsetWithinPacket = this.packetReader.CountOfBytesProcessed - this.owningPacket.PacketByteOffset;

            ReadStreamEventHeader();

            this.ReadStreamEventContext();

            this.Timestamp = this.packetReader.PlaybackCustomization.GetTimestampFromEventHeader(this, this.owningPacket.CurrentEvent?.Timestamp);

            this.DiscardedEvents = this.owningPacket.StreamPacketContext.ReadFieldAsUInt32("events_discarded");

            if (this.owningPacket.PacketTimestampsAreValid)
            {
                // According to CTF Specification 1.8.2, section 5:
                // Time-stamp at the beginning and timestamp at the end of the event packet. Both timestamps are
                // written in the packet header, but sampled respectively while (or before) writing the first event
                // and while (or after) writing the last event in the packet. The inclusive range between these
                // timestamps should include all event timestamps assigned to events contained within the packet. The
                // timestamp at the beginning of an event packet is guaranteed to be below or equal the timestamp at
                // the end of that event packet. The timestamp at the end of an event packet is guaranteed to be below
                // or equal the timestamps at the end of any following packet within the same stream.

                Debug.Assert(this.Timestamp >= this.owningPacket.Start);
                Debug.Assert(this.Timestamp <= this.owningPacket.End);
                if (this.Timestamp < this.owningPacket.Start ||
                    this.Timestamp > this.owningPacket.End)
                {
                    // todo: this could be that the packet and event are using different clocks - handle this case
                    // todo: should we really throw on this? think about how to handle this case, it will take some experience to know what's right
                    throw new CtfPlaybackException(
                        "Corrupt CTF file: event timestamp falls outside of packet boundaries.");
                }
            }
        }

        public void Read()
        {
            this.EventDescriptor = this.packetReader.PlaybackCustomization.GetEventDescriptor(this);
            if (this.EventDescriptor == null)
            {
                throw new CtfPlaybackException("Missing event descriptor for event.");
            }

            if (this.EventDescriptor.Context != null)
            {
                this.Context = this.EventDescriptor.Context.Read(this.packetReader);
            }

            if (this.EventDescriptor.Payload != null)
            {
                ulong startBitCount = this.packetReader.BitsReadFromPacket;
                this.Payload = this.EventDescriptor.Payload.Read(this.packetReader);
                this.PayloadBitCount = (uint) (this.packetReader.BitsReadFromPacket - startBitCount);
            }
        }

        private void ReadStreamEventHeader()
        {
            uint streamIndex = this.owningPacket.StreamId;
            var eventHeaderDescriptor = this.packetReader.Metadata.Streams[(int)streamIndex].EventHeader;
            Debug.Assert(eventHeaderDescriptor != null);

            StreamDefinedEventHeader = eventHeaderDescriptor.Read(this.packetReader);
        }

        private void ReadStreamEventContext()
        {
            uint streamIndex = this.owningPacket.StreamId;
            var eventContextDescriptor = this.packetReader.Metadata.Streams[(int)streamIndex].EventContext;
            if (eventContextDescriptor == null)
            {
                return;
            }

            StreamDefinedEventContext = eventContextDescriptor.Read(this.packetReader);
        }
    }
}