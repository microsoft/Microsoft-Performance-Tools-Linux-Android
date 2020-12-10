// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CtfPlayback.EventStreams.Interfaces;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback
{
    /// <summary>
    /// This class handles all of the nasty bit twiddling needed for unaligned bit access while reading values
    /// from the event stream.
    ///
    /// This is handled here because type alignment is relative to the start of the packet:
    /// specification 1.8.2 section 4.1.2.
    /// </summary>
    internal class PacketReader
        : ICtfEventStream,
          IPacketReader,
          IPacketReaderContext
    {
        /// <summary>
        /// By default, we will attempt to read 1MB at a time.
        /// </summary>
        private const uint DefaultReadSize = 1024 * 1024;

        /// <summary>
        /// The number of bytes we should attempt to read for each buffer fill.
        /// </summary>
        private readonly uint specifiedReadSize;

        /// <summary>
        /// The buffer read from the stream
        /// </summary>
        private readonly byte[] buffer;

        /// <summary>
        /// The count of bytes in the buffer
        /// </summary>
        private uint bufferByteCount;

        /// <summary>
        /// The current byte to read from
        /// </summary>
        private uint bufferByteIndex;

        /// <summary>
        /// The number of bits that have already been read from the current byte
        /// </summary>
        private byte bitsConsumedInCurrentByte;

        private readonly ICtfEventStream eventStream;

        /// <summary>
        /// The total number of bytes read from the stream.
        /// </summary>
        private ulong totalBytesRead;

        public PacketReader(ICtfEventStream eventStream)
            : this(eventStream, DefaultReadSize)
        {
        }

        public PacketReader(ICtfEventStream eventStream, uint bufferByteCount)
            : this(eventStream, new byte[bufferByteCount], bufferByteCount)
        {
        }

        public PacketReader(ICtfEventStream eventStream, byte[] buffer, uint bufferByteCount)
        {
            Debug.Assert(eventStream != null);
            Debug.Assert(buffer != null);

            this.eventStream = eventStream;
            this.buffer = buffer;
            this.bufferByteCount = bufferByteCount;
            this.specifiedReadSize = bufferByteCount;

            // force a read from the stream on the next read
            this.bufferByteIndex = this.bufferByteCount;
        }

        public PacketReader(PacketReader other)
        {
            Debug.Assert(other != null);

            this.eventStream = other.eventStream;
            this.buffer = other.buffer;
            this.bufferByteCount = other.bufferByteCount;
            this.bufferByteIndex = other.bufferByteIndex;
            this.bitsConsumedInCurrentByte = other.bitsConsumedInCurrentByte;
            this.specifiedReadSize = other.specifiedReadSize;
            this.totalBytesRead = other.totalBytesRead;

            this.BitsInPacket = 0;
        }

        public bool EndOfStream { get; private set; }

        /// <summary>
        /// The number of bits available for reading in the current buffer.
        /// </summary>
        public uint RemainingBufferedBitCount
        {
            get
            {
                if (this.bufferByteIndex >= this.bufferByteCount)
                {
                    return 0;
                }

                return (this.bufferByteCount - this.bufferByteIndex) * 8 - this.bitsConsumedInCurrentByte;
            }
        }

        public Stream Stream => this.eventStream.Stream;

        public string StreamSource => this.eventStream.StreamSource;

        public ulong ByteCount => this.eventStream.ByteCount;

        /// <summary>
        /// The number of bytes that have been fully processed from the stream.
        /// </summary>
        public ulong CountOfBytesProcessed
        {
            get
            {
                if (this.totalBytesRead == 0)
                {
                    return 0;
                }

                Debug.Assert(this.totalBytesRead >= this.bufferByteCount);
                return this.totalBytesRead - this.bufferByteCount + this.bufferByteIndex;
            }
        }

        public ICtfMetadata Metadata => this.eventStream.Metadata;

        public ICtfPlaybackCustomization PlaybackCustomization => this.eventStream.PlaybackCustomization;

        /// <summary>
        /// The total number of bits in the packet, including padding.
        /// </summary>
        public ulong BitsInPacket { get; private set; }

        /// <summary>
        /// The number of bits read from the current packet.
        /// </summary>
        public ulong BitsReadFromPacket { get; private set; }

        /// <summary>
        /// Just a helper to check when we need to fill the buffer.
        /// </summary>
        private bool BufferIsEmpty => this.bufferByteIndex >= this.bufferByteCount;

        /// <summary>
        /// Sets the number of bits in the current packet.
        /// <remarks>
        /// The number of bits in the packet often isn't known until some of the packet (header/context) has
        /// been read.
        /// </remarks>
        /// </summary>
        public void SetPacketSize(ulong bitCount)
        {
            this.BitsInPacket = bitCount;
            if (this.BitsReadFromPacket > this.BitsInPacket)
            {
                Debug.Assert(false, $"Read beyond packet before assigning {nameof(BitsInPacket)}.");
                throw new CtfPlaybackException("Read beyond packet.");
            }
        }

        public void ReadToEndOfPacket()
        {
            Debug.Assert(this.BitsInPacket >= this.BitsReadFromPacket);

            ulong bitsToRead = this.BitsInPacket - this.BitsReadFromPacket;
            while (bitsToRead > uint.MaxValue)
            {
                ReadBits(uint.MaxValue);
                bitsToRead -= uint.MaxValue;
            }

            if (bitsToRead > 0)
            {
                ReadBits((uint)bitsToRead);
            }

            if (this.CountOfBytesProcessed == this.ByteCount)
            {
                this.EndOfStream = true;
                this.bufferByteCount = 0;
                this.ResetBuffer();
            }
        }

        public byte[] ReadBits(uint bitCount)
        {
            if (bitCount == 0)
            {
                Debug.Assert(false, "Attempting to read 0 bits... why?");
                return null;
            }

            Debug.Assert(this.bitsConsumedInCurrentByte < 8);
            Debug.Assert(BitsInPacket == 0 || BitsReadFromPacket < BitsInPacket);

            if (BitsInPacket > 0 && BitsInPacket - BitsReadFromPacket < bitCount)
            {
                // attempt to read past the end of the packet
                Debug.Assert(false, "Attempt to read beyond packet.");
                throw new CtfPlaybackException($"Event data is corrupt. Attempt to read beyond end of packet.");
            }

            if (this.BufferIsEmpty)
            {
                Debug.Assert(this.bitsConsumedInCurrentByte == 0);
                this.FillBuffer();
            }

            this.BitsReadFromPacket += bitCount;

            if (this.bitsConsumedInCurrentByte == 0)
            {
                return this.FastRead(bitCount);
            }

            return this.SlowRead(bitCount);
        }

        public byte[] ReadString()
        {
            // According to spec 1.8.2 section 4.2.5, strings are always byte-aligned.
            // This should have been aligned before calling this method.
            Debug.Assert(this.bitsConsumedInCurrentByte == 0);

            // Confirm that we haven't already gone beyond end of stream.
            Debug.Assert(this.BitsInPacket == 0 || (this.BitsInPacket > this.BitsReadFromPacket));

            var returnValue = new List<byte>();

            // read until a byte of 0 is returned
            while(true)
            {
                if (this.BitsInPacket > 0 && ((this.BitsInPacket - this.BitsReadFromPacket) < 8))
                {
                    // read past the end of the packet
                    Debug.Assert(false, "Attempt to read beyond packet.");
                    throw new CtfPlaybackException($"Event data is corrupt. Read beyond end of packet while reading a string value.");
                }

                if (this.BufferIsEmpty)
                {
                    Debug.Assert(this.bitsConsumedInCurrentByte == 0);
                    this.FillBuffer();
                }

                byte currentByte = this.buffer[this.bufferByteIndex];
                this.bufferByteIndex++;
                this.BitsReadFromPacket += 8;
                returnValue.Add(currentByte);

                if (currentByte == 0)
                {
                    return returnValue.ToArray();
                }
            }
        }

        public void Align(uint bitCount)
        {
            // Confirm that we haven't already gone beyond end of stream.
            Debug.Assert(this.BitsInPacket == 0 || (this.BitsInPacket >= this.BitsReadFromPacket));

            // Check if we're already aligned
            ulong currentBitOffset = this.BitsReadFromPacket;
            uint remainder = (uint)(currentBitOffset % bitCount);
            if (remainder == 0)
            {
                return;
            }

            // Not aligned yet, so how many bits are needed for alignment
            uint bitsToProgress = bitCount - remainder;
            if (this.BitsInPacket - bitsToProgress < this.BitsReadFromPacket)
            {
                // we've unexpectedly hit the end of the stream
                throw new CtfPlaybackException("Aligning stream progresses beyond the current packet.");
            }

            this.BitsReadFromPacket += bitsToProgress;

            // Handle some special cases...
            if (this.bitsConsumedInCurrentByte > 0)
            {
                byte freeBitsInCurrentByte = (byte) (8 - this.bitsConsumedInCurrentByte);

                // We're staying withing the current byte, so not much to do... weird case though
                if (bitsToProgress < freeBitsInCurrentByte)
                {
                    this.bitsConsumedInCurrentByte += (byte)bitsToProgress;
                    return;
                }

                // Bits to progress goes at least to the end of the current byte
                this.bitsConsumedInCurrentByte = 0;
                this.bufferByteIndex++;

                // That's it, no more to do in this case. Just moved to the start of the next byte
                if (bitsToProgress == freeBitsInCurrentByte)
                {
                    return;
                }

                // Still more to do, but we consumed this many bits already
                bitsToProgress -= freeBitsInCurrentByte;
            }

            // progress by entire bytes, we should be byte aligned at this point
            Debug.Assert(this.bitsConsumedInCurrentByte == 0);
            while (bitsToProgress > 8)
            {
                if (this.bufferByteIndex + 1 >= this.bufferByteCount)
                {
                    this.FillBuffer();
                }
                else
                {
                    this.bufferByteIndex++;
                }

                bitsToProgress -= 8;
            }

            Debug.Assert(bitsToProgress < 8);
            Debug.Assert(this.bitsConsumedInCurrentByte == 0);
            if (bitsToProgress > 0)
            {
                if (this.bufferByteIndex >= this.bufferByteCount)
                {
                    this.FillBuffer();
                }

                this.bitsConsumedInCurrentByte += (byte)bitsToProgress;
            }
        }

        /// <summary>
        /// This is called when working from a byte-aligned buffer.
        /// </summary>
        /// <param name="bitCount">Number of bits to read</param>
        /// <returns>Data read</returns>
        private byte[] FastRead(uint bitCount)
        {
            Debug.Assert(this.bitsConsumedInCurrentByte == 0);

            uint bytesToRead = ByteCountFromBitCount(bitCount);

            byte[] returnBuffer = new byte[bytesToRead];
            uint returnBufferIndex = 0;

            // progress by entire bytes, we should be byte aligned at this point
            while (bitCount >= 8)
            {
                if (this.bufferByteIndex >= this.bufferByteCount)
                {
                    this.FillBuffer();
                }

                returnBuffer[returnBufferIndex++] = this.buffer[this.bufferByteIndex++];
                bitCount -= 8;
            }

            if (bitCount > 0)
            {
                Debug.Assert(bitCount < 8);
                Debug.Assert(returnBufferIndex == returnBuffer.Length - 1);

                int oneBitBeyondBitsUsed = 2 << ((byte)bitCount - 1);
                int bitMask = oneBitBeyondBitsUsed - 1;

                returnBuffer[returnBufferIndex] = (byte)(this.buffer[this.bufferByteIndex] & bitMask);
                this.bitsConsumedInCurrentByte = (byte)bitCount;
            }

            return returnBuffer;
        }

        /// <summary>
        /// This is called when not reading from a byte-aligned buffer.
        /// </summary>
        /// <param name="bitCount">The number of bits to read</param>
        /// <returns>Data read</returns>
        private byte[] SlowRead(uint bitCount)
        {
            uint targetByteCount = ByteCountFromBitCount(bitCount);

            byte[] targetBuffer = new byte[targetByteCount];
            uint targetByteIndex = 0;
            byte usedBitsInTargetByte = 0;

            while (bitCount > 0)
            {
                if (this.bufferByteIndex >= this.bufferByteCount)
                {
                    this.FillBuffer();
                }

                byte bitsFromSourceByte = (byte)(8 - this.bitsConsumedInCurrentByte);
                byte bitsInTargetByte = (byte)(8 - usedBitsInTargetByte);

                byte bitsToProcess = Math.Min(bitsFromSourceByte, bitsInTargetByte);
                bitsToProcess = (byte) Math.Min(bitsToProcess, bitCount);

                byte sourceMask = (byte)(1 << this.bitsConsumedInCurrentByte);
                byte targetMask = (byte)(1 << usedBitsInTargetByte);
                for (byte x = 0; x < bitsToProcess; x++)
                {
                    byte maskedSourceByte = (byte)(this.buffer[this.bufferByteIndex] & sourceMask);
                    if (maskedSourceByte != 0)
                    {
                        targetBuffer[targetByteIndex] |= targetMask;
                    }

                    sourceMask = (byte)(sourceMask << 1);
                    targetMask = (byte)(targetMask << 1);

                    this.bitsConsumedInCurrentByte++;
                    usedBitsInTargetByte++;

                    bitCount--;
                }

                Debug.Assert(this.bitsConsumedInCurrentByte <= 8);
                Debug.Assert(usedBitsInTargetByte <= 8);

                if (this.bitsConsumedInCurrentByte == 8)
                {
                    this.bitsConsumedInCurrentByte = 0;
                    this.bufferByteIndex++;
                }

                if (usedBitsInTargetByte == 8)
                {
                    usedBitsInTargetByte = 0;
                    targetByteIndex++;
                }
            }

            return targetBuffer;
        }

        private uint ByteCountFromBitCount(uint bitCount)
        {
            uint byteCount = bitCount >> 3;

            if ((bitCount & 0x7) != 0)
            {
                byteCount++;
            }

            return byteCount;
        }

        private void ResetBuffer()
        {
            this.bufferByteIndex = 0;
            this.bitsConsumedInCurrentByte = 0;
        }

        private void FillBuffer(bool expectData = true)
        {
            Debug.Assert(this.bitsConsumedInCurrentByte == 0);
            Debug.Assert(!this.EndOfStream);

            this.ResetBuffer();
            this.bufferByteCount = (uint) this.eventStream.Stream.Read(this.buffer, 0, (int) this.specifiedReadSize);

            this.totalBytesRead += this.bufferByteCount;

            if (this.bufferByteCount == 0)
            {
                Debug.Assert(this.totalBytesRead == this.ByteCount);
                this.EndOfStream = true;
            }

            if (this.bufferByteCount == 0 && expectData)
            {
                if (this.bufferByteCount == 0)
                {
                    throw new CtfPlaybackException("Unexpected end-of-stream found.");
                }
            }
        }

        public void Dispose()
        {
            // This is just a wrapper around the object that own the event stream
        }
    }
}