// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using System.Threading;
using CtfPlayback.EventStreams;
using CtfPlayback.EventStreams.Interfaces;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback
{
    internal class CtfStreamPlayback
    {
        private readonly CancellationToken cancellationToken;
        private readonly bool useReadAhead;

        private PacketReader packetReader;
        private CtfStreamReadAhead streamReader;

        public CtfStreamPlayback(
            ICtfEventStream eventStream, 
            CtfPlaybackOptions playbackOptions, 
            CancellationToken cancellationToken)
        {
            Debug.Assert(eventStream != null);
            Debug.Assert(cancellationToken != null);

            this.cancellationToken = cancellationToken;
            this.EventStream = eventStream;
            this.useReadAhead = playbackOptions.ReadAhead;
        }

        public ICtfPacket CurrentPacket { get; private set; }

        public ICtfEvent CurrentEvent { get; private set; }

        public ICtfEventStream EventStream { get; }

        public Stream Stream => this.EventStream.Stream;

        public ICtfMetadata Metadata => this.EventStream.Metadata;

        public ulong CountOfBytesProcessed { get; private set; }

        public bool MoveToNextEvent()
        {
            return this.useReadAhead ? this.MoveToNextEventWithReadAhead() : this.MoveToNextEventWithoutReadAhead();
        }

        public bool MoveToNextPacket()
        {
            if(this.packetReader != null)
            {
                if (this.packetReader.EndOfStream && this.packetReader.RemainingBufferedBitCount == 0)
                {
                    return false;
                }

                this.packetReader = new PacketReader(this.packetReader);
            }
            else
            {
                this.packetReader = new PacketReader(this.EventStream);
            }

            var packet = new CtfPacket(this.packetReader, this.CurrentPacket?.CurrentEvent);
            if (!packet.ReadPacketMetadata())
            {
                return false;
            }

            this.CurrentPacket = packet;
            return true;
        }

        private bool MoveToNextEventWithReadAhead()
        {
            if (this.streamReader == null)
            {
                this.streamReader = new CtfStreamReadAhead(this.EventStream, this.cancellationToken);
                var t = new Thread(this.streamReader.ReadEventStream);
                t.Name = "CTF ReadAhead";
                t.Start();
            }

            bool readEvent = this.streamReader.PopEvent(
                out ICtfEvent ctfEvent,
                out ICtfPacket ctfPacket,
                out ulong bytesConsumed);
            if (!readEvent)
            {
                // done reading the stream
                return false;
            }

            this.CurrentEvent = ctfEvent;
            this.CurrentPacket = ctfPacket;
            this.CountOfBytesProcessed = bytesConsumed;
            return true;
        }

        private bool MoveToNextEventWithoutReadAhead()
        {
            //// On the very first call to this method, we'll need to read the first packet
            if (this.CurrentPacket == null)
            {
                if (!this.MoveToNextPacket())
                {
                    return false;
                }

                Debug.Assert(this.CurrentPacket != null);
            }

            // If we cannot move to the next event within a packet, it's time to move to
            // the next packet.
            while (!this.CurrentPacket.MoveToNextEvent())
            {
                if (!this.MoveToNextPacket())
                {
                    // stream is complete
                    return false;
                }
            }

            this.CurrentEvent = this.CurrentPacket.CurrentEvent;
            this.CountOfBytesProcessed = this.packetReader.CountOfBytesProcessed;

            return true;
        }
    }
}