// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using CtfPlayback.EventStreams.Interfaces;
using CtfPlayback.FieldValues;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.EventStreams
{
    /// <inheritdoc />
    /// <summary>
    /// A CTF Packet is a container for events, along with some metadata at the beginning of the packet.
    /// This class provides a way to read the metadata, and to iterate over its events.
    /// </summary>
    internal class CtfPacket
        : ICtfPacket
    {
        private readonly PacketReader packetReader;

        public CtfPacket(PacketReader packetReader, ICtfEvent previousEvent)
        {
            this.packetReader = packetReader;

            // This is set for the sake of timestamps. The first timestamp in this packet might be
            // less than 64-bits, and so requires the last timestamp of the stream to complete it.
            // See CTF specification 1.8.2 section 8 "Clocks" - and read carefully about N-bit integer
            // types referring to clocks.
            //
            this.CurrentEvent = previousEvent;

            Debug.Assert(this.packetReader.Metadata.TraceDescriptor.PacketHeader != null);
        }

        public CtfTimestamp Start { get; private set; }

        public CtfTimestamp End { get; private set; }

        public ulong PacketByteOffset { get; private set; }

        public bool PacketTimestampsAreValid { get; private set; }

        public ICtfEvent CurrentEvent { get; private set; }

        public CtfStructValue TracePacketHeader { get; private set; }

        public CtfStructValue StreamPacketContext { get; private set; }

        public Stream Stream => this.packetReader.Stream;

        public ICtfMetadata Metadata => this.packetReader.Metadata;

        public uint StreamId { get; private set; }

        public ulong BitsInPacketContent { get; private set; }

        public bool ReadPacketMetadata()
        {
            this.PacketByteOffset = this.packetReader.CountOfBytesProcessed;

            ReadPacketHeader();
            ReadPacketContext();

            this.DetermineStreamIndex();

            this.packetReader.SetPacketSize(this.packetReader.PlaybackCustomization.GetBitsInPacket(this));

            this.BitsInPacketContent = 
                this.packetReader.PlaybackCustomization.GetPacketContentBitCount(this);

            this.PacketTimestampsAreValid = false;

            if (this.packetReader.PlaybackCustomization.GetTimestampsFromPacketContext(
                this, out var startValue, out var endValue))
            {
                this.Start = startValue;
                this.End = endValue;
                this.PacketTimestampsAreValid = true;
            }

            return true;
        }

        public bool MoveToNextEvent()
        {
            if (GetRemainingBits() == 0)
            {
                // read to end of the packet (alignment)
                this.packetReader.ReadToEndOfPacket();
                return false;
            }

            var nextEvent = new CtfEvent(this.packetReader, this);
            nextEvent.ReadEventMetadata();

            this.CurrentEvent = nextEvent;

            this.CurrentEvent.Read();

            return true;
        }

        private void ReadPacketHeader()
        {
            var packetHeaderObject = this.Metadata.TraceDescriptor.PacketHeader.Read(this.packetReader);
            if (!(packetHeaderObject is CtfStructValue packetHeaderStruct))
            {
                // We should never have to here, this should be caught earlier.
                throw new Exception("The trace.packet.header is not a structure.");
            }

            this.TracePacketHeader = packetHeaderStruct;
        }

        private void ReadPacketContext()
        {
            this.StreamPacketContext = this.Metadata.Streams[(int)this.StreamId].PacketContext.Read(this.packetReader) as CtfStructValue;
        }

        private ulong GetRemainingBits()
        {
            Debug.Assert(this.BitsInPacketContent >= this.packetReader.BitsReadFromPacket);
            if (this.BitsInPacketContent < this.packetReader.BitsReadFromPacket)
            {
                return 0;
            }
            return this.BitsInPacketContent - this.packetReader.BitsReadFromPacket;
        }

        private void DetermineStreamIndex()
        {
            var streamIdDescriptor = this.Metadata.TraceDescriptor?.PacketHeader?.GetField("stream_id");
            if (streamIdDescriptor == null)
            {
                this.StreamId = 0;
            }

            bool streamIdFound = this.TracePacketHeader.FieldsByName.TryGetValue("stream_id", out var streamIdFieldValue);
            Debug.Assert(streamIdFound);

            var streamIdIntegerValue = streamIdFieldValue as CtfIntegerValue;
            Debug.Assert(streamIdIntegerValue != null);

            if (!streamIdIntegerValue.Value.TryGetUInt32(out var streamId))
            {
                throw new CtfPlaybackException("The stream_id is not a uint.");
            }

            this.StreamId = streamId;
        }
    }
}