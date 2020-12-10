// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.EventStreams.Interfaces
{
    /// <summary>
    /// Represents an event from a CTF event stream.
    /// </summary>
    public interface ICtfEvent
    {
        /// <summary>
        /// For debugging purposes.
        /// </summary>
        ulong ByteOffsetWithinPacket { get; }

        /// <summary>
        /// Timestamp associated with the event
        /// </summary>
        CtfTimestamp Timestamp { get; }

        /// <summary>
        /// If stream.event.header is defined, this is that value read from the event.
        /// </summary>
        CtfFieldValue StreamDefinedEventHeader { get; }

        /// <summary>
        /// If stream.event.context is defined, this is that value read from the event.
        /// </summary>
        CtfFieldValue StreamDefinedEventContext { get; }

        /// <summary>
        /// If an event context is defined, this is that value read from the event.
        /// </summary>
        CtfFieldValue Context { get; }

        /// <summary>
        /// The event payload value read from the event.
        /// </summary>
        CtfFieldValue Payload { get; }

        /// <summary>
        /// The number of bits consumed from the stream for Payload.
        /// </summary>
        uint PayloadBitCount { get; }

        /// <summary>
        /// Number of discarded events so far.
        /// </summary>
        uint DiscardedEvents { get; }

        /// <summary>
        /// The metadata descriptor associated with this event.
        /// </summary>
        ICtfEventDescriptor EventDescriptor { get; }

        /// <summary>
        /// Read StreamDefinedEventHeader and StreamDefinedEventContext.
        /// </summary>
        void ReadEventMetadata();

        /// <summary>
        /// Read Context and Payload.
        /// </summary>
        void Read();
    }
}