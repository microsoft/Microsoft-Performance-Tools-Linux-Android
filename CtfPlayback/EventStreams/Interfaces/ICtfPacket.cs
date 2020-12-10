// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;

namespace CtfPlayback.EventStreams.Interfaces
{
    ///  <summary>
    ///  A packet consists of:
    ///     event.packet.header
    ///       - The only required field in this structure is stream_id, and this is only required if there are
    ///         multiple streams in the trace.
    ///       - Recommended fields include:
    ///          uint32_t magic    : 0xC1FC1FC1
    ///          uint8_t  uuid[16] : trace identifier
    ///     stream.packet.context
    ///       - fields ending in "_begin" and "_end" have special meaning (spec 1.8.2 section 8).
    ///         these are the timestamps at the beginning and ending of this packet.
    ///         the beginning timestamp is guaranteed to be less than or equal to the ending timestamp
    ///       - all fields are optional
    ///       - if packet_size is missing, the stream contains just this single packet (spec 1.8.2 section 5.2)
    ///           = content_size + size of padding
    ///       - if content_size is missing, there is no padding in the packet
    ///     zero or more events
    ///  Note that all type alignment values are based on the start of a packet. (spec 1.8.2 section 4.1.2)
    ///  </summary>
    public interface ICtfPacket
    {
        /// <summary>
        /// Used for debugging purposes.
        /// Byte offset of the packet within the stream.
        /// </summary>
        ulong PacketByteOffset { get; }

        /// <summary>
        /// Determines if the <see cref="Start"/> and <see cref="End"/> Timestamp properties are valid.
        /// </summary>
        bool PacketTimestampsAreValid { get; }

        /// <summary>
        /// The start timestamp for the packet.
        /// </summary>
        CtfTimestamp Start { get; }

        /// <summary>
        /// The end timestamp for the packet.
        /// </summary>
        CtfTimestamp End { get; }

        /// <summary>
        /// The event within the packet currently read.
        /// </summary>
        ICtfEvent CurrentEvent { get; }

        /// <summary>
        /// The stream id associated with the packet.
        /// </summary>
        uint StreamId { get; }

        /// <summary>
        /// The trace.packet.header value in the packet.
        /// </summary>
        CtfStructValue TracePacketHeader { get; }

        /// <summary>
        /// The stream.packet.context value in the packet.
        /// </summary>
        CtfStructValue StreamPacketContext { get; }

        /// <summary>
        /// Reads metadata at the beginning of the packet.
        /// </summary>
        /// <returns>True if success</returns>
        bool ReadPacketMetadata();

        /// <summary>
        /// Reads in the next event
        /// </summary>
        /// <returns>True if success</returns>
        bool MoveToNextEvent();
    }
}