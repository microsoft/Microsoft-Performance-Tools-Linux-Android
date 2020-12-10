// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CtfPlayback.EventStreams;
using CtfPlayback.EventStreams.Interfaces;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback
{
    internal class CtfStreamReadAhead
    {
        private readonly CancellationToken cancellationToken;
        private const int QueueSizeLimit = 20;

        private PacketReader packetReader;
        private readonly ManualResetEventSlim dataReadyForConsumption = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim readyForNewData = new ManualResetEventSlim(true);

        private bool endOfStream = false;

        public CtfStreamReadAhead(ICtfEventStream eventStream, CancellationToken cancellationToken)
        {
            Debug.Assert(eventStream != null);
            Debug.Assert(cancellationToken != null);

            this.cancellationToken = cancellationToken;
            this.EventStream = eventStream;
        }

        public ICtfEventStream EventStream { get; }

        public Stream Stream => this.EventStream.Stream;

        public ICtfMetadata Metadata => this.EventStream.Metadata;

        public ICtfPacket CurrentPacket { get; private set; }

        private Queue<ICtfEvent> Events { get; } = new Queue<ICtfEvent>(QueueSizeLimit);

        private Queue<ICtfPacket> Packets { get; } = new Queue<ICtfPacket>(QueueSizeLimit);

        private Queue<ulong> BytesProcessed { get; } = new Queue<ulong>(QueueSizeLimit);

        public bool PopEvent(out ICtfEvent ctfEvent, out ICtfPacket ctfPacket, out ulong bytesConsumed)
        {
            this.dataReadyForConsumption.Wait(this.cancellationToken);
            this.cancellationToken.ThrowIfCancellationRequested();

            lock (this.Events)
            {
                if (this.Events.Count == 0)
                {
                    // we've reached the end of the stream
                    ctfEvent = null;
                    ctfPacket = null;
                    bytesConsumed = 0;
                    return false;
                }

                ctfEvent = this.Events.Dequeue();
                ctfPacket = this.Packets.Dequeue();
                bytesConsumed = this.BytesProcessed.Dequeue();

                if (this.Events.Count == 0 && !this.endOfStream)
                {
                    this.dataReadyForConsumption.Reset();
                }

                this.readyForNewData.Set();
            }

            return true;
        }

        public void ReadEventStream()
        {
            while (this.MoveToNextEvent())
            {
                PushEvent(this.CurrentPacket.CurrentEvent, this.CurrentPacket, this.packetReader.CountOfBytesProcessed);
            }

            lock (this.Events)
            {
                this.endOfStream = true;
                this.dataReadyForConsumption.Set();
            }
        }

        private void PushEvent(ICtfEvent ctfEvent, ICtfPacket ctfPacket, ulong bytesConsumed)
        {
            Debug.Assert(ctfEvent != null);
            Debug.Assert(ctfPacket != null);
            Debug.Assert(bytesConsumed != 0);

            this.readyForNewData.Wait(this.cancellationToken);
            this.cancellationToken.ThrowIfCancellationRequested();

            lock (this.Events)
            {
                this.Events.Enqueue(ctfEvent);
                this.Packets.Enqueue(ctfPacket);
                this.BytesProcessed.Enqueue(bytesConsumed);

                if (this.Events.Count >= QueueSizeLimit)
                {
                    this.readyForNewData.Reset();
                }

                this.dataReadyForConsumption.Set();
            }
        }

        private bool MoveToNextEvent()
        {
            // On the very first call to this method, we'll need to read the first packet
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

            return true;
        }

        private bool MoveToNextPacket()
        {
            if (this.packetReader != null)
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
    }
}