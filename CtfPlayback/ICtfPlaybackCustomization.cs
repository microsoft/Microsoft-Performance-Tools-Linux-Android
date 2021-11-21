// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.EventStreams.Interfaces;
using CtfPlayback.FieldValues;
using CtfPlayback.Inputs;
using CtfPlayback.Metadata;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback
{
    /// <summary>
    /// This interface provides extensibility points for specific CTF implementations.
    /// </summary>
    public interface ICtfPlaybackCustomization
    {
        /// <summary>
        /// Additional customization for parsing metadata.
        /// </summary>
        ICtfMetadataCustomization MetadataCustomization { get; }

        /// <summary>
        /// A customized metadata parser may be supplied.
        /// </summary>
        /// <param name="metadataStream"></param>
        /// <returns></returns>
        ICtfMetadataParser CreateMetadataParser(ICtfTraceInput metadataStream);

        /// <summary>
        /// Allows for customized parsing of a packet to determine its start and end timestamps.
        /// </summary>
        /// <param name="ctfPacket"></param>
        /// <param name="metadata"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        bool GetTimestampsFromPacketContext(
            ICtfPacket ctfPacket,
            ICtfMetadata metadata,
            out CtfTimestamp start,
            out CtfTimestamp end);

        /// <summary>
        /// Allows for customized parsing of an event header data to determine a timestamp
        ///  for the event.
        /// </summary>
        CtfTimestamp GetTimestampFromEventHeader(ICtfEvent ctfEvent, ICtfMetadata metadata, CtfTimestamp previousTimestamp);

        /// <summary>
        /// Used to retrieve the total number of bits in a packet.
        /// </summary>
        /// <param name="ctfPacket">The packet with its header and context</param>
        /// <returns>Bit in the packet, including alignment padding</returns>
        ulong GetBitsInPacket(ICtfPacket ctfPacket);

        /// <summary>
        /// Used to retrieve the number of bits in a packet, minus alignment.
        /// </summary>
        /// <param name="ctfPacket">The packet with its header and context</param>
        /// <returns>Bit in the packet, minus alignment padding</returns>
        ulong GetPacketContentBitCount(ICtfPacket ctfPacket);

        /// <summary>
        /// Reads the event at the current location from packetReader 
        /// </summary>
        /// <param name="ctfEvent">Event with its context and header filled in</param>
        /// <param name="metadata">The metadata for the event</param>
        /// <returns></returns>
        ICtfEventDescriptor GetEventDescriptor(ICtfEvent ctfEvent, ICtfMetadata metadata);

        /// <summary>
        /// Called during trace playback for the current event.
        /// </summary>
        /// <param name="ctfEvent">Event to process</param>
        /// <param name="eventPacket">Packet which contains the event</param>
        /// <param name="ctfTraceInput">Trace which contains the event</param>
        /// <param name="ctfEventStream">Stream which contains the event</param>
        /// <param name="metadata">The metadata for the event</param>
        void ProcessEvent(ICtfEvent ctfEvent, ICtfPacket eventPacket, ICtfTraceInput ctfTraceInput, ICtfInputStream ctfEventStream, ICtfMetadata metadata);
    }
}