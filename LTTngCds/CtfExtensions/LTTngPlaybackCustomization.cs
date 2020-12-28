// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using CtfPlayback;
using CtfPlayback.EventStreams.Interfaces;
using CtfPlayback.FieldValues;
using CtfPlayback.Inputs;
using CtfPlayback.Metadata;
using CtfPlayback.Metadata.AntlrParser;
using CtfPlayback.Metadata.Interfaces;
using LTTngCds.CookerData;
using LTTngCds.CtfExtensions.Descriptors;

namespace LTTngCds.CtfExtensions
{
    internal class LTTngPlaybackCustomization
        : ICtfPlaybackCustomization
    {
        private readonly LTTngMetadataCustomization metadataCustomization;
        private readonly List<Action<LTTngEvent, LTTngContext>> eventCallbacks = 
            new List<Action<LTTngEvent, LTTngContext>>();

        private readonly Dictionary<ICtfInputStream, uint> streamToCpu = new Dictionary<ICtfInputStream, uint>();

        private readonly Dictionary<ICtfTraceInput, TraceContext> traceContexts = new Dictionary<ICtfTraceInput, TraceContext>();

        private ulong eventNumber;

        public LTTngPlaybackCustomization(ICtfInput traceInput)
        {
            this.metadataCustomization = new LTTngMetadataCustomization(traceInput.Traces.Count);
        }

        public ICtfMetadataCustomization MetadataCustomization => this.metadataCustomization;

        public virtual ICtfMetadataParser CreateMetadataParser(
            ICtfTraceInput traceInput)
        {
            this.metadataCustomization.PrepareForNewTrace(traceInput.MetadataStream);
            return new CtfAntlrMetadataParser(this.metadataCustomization, this.metadataCustomization.LTTngMetadata);
        }

        public bool GetTimestampsFromPacketContext(
            ICtfPacket ctfPacket, 
            out CtfTimestamp start, 
            out CtfTimestamp end)
        {
            start = null;
            end = null;

            if (!(ctfPacket.StreamPacketContext is CtfStructValue packetContextStruct))
            {
                throw new LTTngPlaybackException("The stream.packet.context is not a structure.");
            }

            if (!packetContextStruct.FieldsByName.TryGetValue("timestamp_begin", out var startFieldValue))
            {
                return false;
            }

            if (!packetContextStruct.FieldsByName.TryGetValue("timestamp_end", out var endFieldValue))
            {
                return false;
            }

            if (!(startFieldValue is CtfIntegerValue startFieldInteger))
            {
                return false;
            }

            if (!(endFieldValue is CtfIntegerValue endFieldInteger))
            {
                return false;
            }

            start = new CtfTimestamp(this.MetadataCustomization, startFieldInteger);
            end = new CtfTimestamp(this.MetadataCustomization, endFieldInteger);
            return true;
        }

        public void RegisterEventCallback(Action<LTTngEvent, LTTngContext> eventCallback)
        {
            this.eventCallbacks.Add(eventCallback);
        }

        public virtual CtfTimestamp GetTimestampFromEventHeader(ICtfEvent ctfEvent, CtfTimestamp previousTimestamp)
        {
            var variantStruct = GetStreamEventHeaderStructure(ctfEvent);
            Debug.Assert(variantStruct != null);

            if (!variantStruct.FieldsByName.TryGetValue("timestamp", out var timestampValue))
            {
                throw new LTTngPlaybackException("stream.event.header variant 'v' struct has no 'timestamp' field");
            }

            if (!(timestampValue is CtfIntegerValue timestampInteger))
            {
                throw new LTTngPlaybackException("stream.event.header variant 'v' struct field 'timestamp'" + 
                                               " is not an integer");
            }

            if (previousTimestamp is null)
            {
                // todo:I think we need to do something else for this case, especially if the integer size is < 64
                // not sure what yet. maybe base it on the clock's offset?

                return new CtfTimestamp(this.MetadataCustomization, timestampInteger);
            }

            // Timestamps aren't actually absolute values. To quote from CTF spec 1.8.2 section 8:
            //    For a N-bit integer type referring to a clock, if the integer overflows compared to the N low order bits
            //    of the clock prior value found in the same stream, then it is assumed that one, and only one, overflow
            //    occurred. It is therefore important that events encoding time on a small number of bits happen frequently
            //    enough to detect when more than one N-bit overflow occurs.
            // So to determine a timestamp, we must know the previous timestamp. If they're all the same number of bits, it
            // wouldn't be necessary (I don't think so anyways). But some timestamps are smaller than others.
            if (timestampInteger.Descriptor.Size < 64)
            {
                if (!timestampInteger.Value.TryGetInt64(out long thisTimestamp))
                {
                    Debug.Assert(false);
                    throw new CtfPlaybackException("Unable to retrieve timestamp as long.");
                }

                long previous = (long)previousTimestamp.NanosecondsFromClockBase;

                long oneBitBeyondBitsUsed = 1L << (byte)timestampInteger.Descriptor.Size;
                long bitMask = ~(oneBitBeyondBitsUsed - 1);

                long highBitsFromPreviousTimestamp = previous & bitMask;
                long newTimestamp = highBitsFromPreviousTimestamp | thisTimestamp;
                if (newTimestamp < previous)
                {
                    // handle the overflow case
                    newTimestamp += oneBitBeyondBitsUsed;
                    Debug.Assert(newTimestamp > previous);
                }

                return new CtfTimestamp(this.MetadataCustomization, timestampInteger, newTimestamp);
            }

            return new CtfTimestamp(this.MetadataCustomization, timestampInteger);
        }

        /// <inheritdoc />
        public virtual ulong GetBitsInPacket(ICtfPacket ctfPacket)
        {
            CtfFieldValue streamPacketContext = ctfPacket.StreamPacketContext;
            if (streamPacketContext == null)
            {
                throw new LTTngPlaybackException("The stream.packet.context field is not set.");
            }

            if (!(streamPacketContext is CtfStructValue packetContext))
            {
                throw new LTTngPlaybackException("The stream.packet.context is not a structure.");
            }

            if (!packetContext.FieldsByName.ContainsKey("packet_size"))
            {
                throw new LTTngPlaybackException(
                    "Field packet_size was not found in the stream.packet.context structure.");
            }

            var fieldValue = packetContext.FieldsByName["packet_size"];
            if (!(fieldValue is CtfIntegerValue packetSize))
            {
                throw new LTTngPlaybackException(
                    "Field packet_size was is not an integer value.");
            }

            if (!packetSize.Value.TryGetUInt64(out var bitCount))
            {
                throw new LTTngPlaybackException(
                    "Field packet_size is not a valid ulong value.");
            }

            return bitCount;
        }

        /// <inheritdoc />
        public ulong GetPacketContentBitCount(ICtfPacket ctfPacket)
        {
            CtfFieldValue streamPacketContext = ctfPacket.StreamPacketContext;
            if (streamPacketContext == null)
            {
                throw new LTTngPlaybackException("The stream.packet.context field is not set.");
            }

            if (!(streamPacketContext is CtfStructValue packetContext))
            {
                throw new LTTngPlaybackException("The stream.packet.context is not a structure.");
            }

            if (!packetContext.FieldsByName.ContainsKey("content_size"))
            {
                throw new LTTngPlaybackException(
                    "Field packet_size was not found in the stream.packet.context structure.");
            }

            var fieldValue = packetContext.FieldsByName["content_size"];
            if (!(fieldValue is CtfIntegerValue contentSize))
            {
                throw new LTTngPlaybackException(
                    "Field content_size was is not an integer value.");
            }

            if (!contentSize.Value.TryGetUInt64(out var bitCount))
            {
                throw new LTTngPlaybackException(
                    "Field content_size is not a valid ulong value.");
            }

            return bitCount;
        }

        /// <inheritdoc />
        public ICtfEventDescriptor GetEventDescriptor(
            ICtfEvent ctfEvent)
        {
            uint id = this.GetEventId(ctfEvent);

            if (!this.metadataCustomization.LTTngMetadata.EventByEventId.TryGetValue(id, out var eventDescriptor))
            {
                throw new LTTngPlaybackException($"Unable to find event descriptor for event id={id}.");
            }

            return eventDescriptor;
        }

        public void ProcessEvent(ICtfEvent ctfEvent, ICtfPacket eventPacket, ICtfTraceInput traceInput, ICtfInputStream ctfEventStream)
        {
            var eventDescriptor = ctfEvent.EventDescriptor as EventDescriptor;
            Debug.Assert(eventDescriptor != null);
            if (eventDescriptor == null)
            {
                throw new LTTngPlaybackException("EventDescriptor is not an LTTng descriptor.");
            }

            if (!this.streamToCpu.TryGetValue(ctfEventStream, out var cpuId))
            {
                var cpuIndex = ctfEventStream.StreamSource.LastIndexOf('_');
                string cpu = ctfEventStream.StreamSource.Substring(cpuIndex + 1);
                if (!uint.TryParse(cpu, out cpuId))
                {
                    Debug.Assert(false, "Unable to parse cpu from LTTng stream channel");
                    cpuId = uint.MaxValue;
                }

                this.streamToCpu.Add(ctfEventStream, cpuId);
            }

            if (!this.traceContexts.TryGetValue(traceInput, out var traceContext))
            {
                traceContext = new TraceContext(this.metadataCustomization.LTTngMetadata);
                this.traceContexts.Add(traceInput, traceContext);
            }
            
            var callbackEvent = new LTTngEvent(ctfEvent);

            var callbackContext = new LTTngContext(this.metadataCustomization, ctfEventStream, traceContext)
            {
                // todo: when supporting multiple traces, this timestamp needs to become relative to the earliest timestamp all traces
                // todo: when supporting multiple traces, this one event number needs to become cumulative across traces, and one specific to the current trace
                CurrentCpu = cpuId,
                Timestamp = (long)ctfEvent.Timestamp.NanosecondsFromClockBase,/// - this.baseTimestamp,
                CurrentEventNumber = this.eventNumber,
                CurrentEventNumberWithinTrace = this.eventNumber,
            };

            foreach (var callback in this.eventCallbacks)
            {
                callback(callbackEvent, callbackContext);
            }

            ++this.eventNumber;
        }

        private CtfVariantValue GetStreamEventHeaderVariant(ICtfEvent ctfEvent)
        {
            if (ctfEvent.StreamDefinedEventHeader == null)
            {
                throw new LTTngPlaybackException("stream.event.header is not defined");
            }

            if (!(ctfEvent.StreamDefinedEventHeader is CtfStructValue eventHeaderStruct))
            {
                throw new LTTngPlaybackException("stream.event.header is not a structure");
            }

            if (!eventHeaderStruct.FieldsByName.TryGetValue("v", out var variant))
            {
                throw new LTTngPlaybackException("stream.event.header does not contain field 'v'");
            }

            if (!(variant is CtfVariantValue variantValue))
            {
                throw new LTTngPlaybackException("stream.event.header does not field 'v' is not a variant");
            }

            return variantValue;
        }

        private CtfStructValue GetStreamEventHeaderStructure(CtfVariantValue eventVariant)
        {
            if (!(eventVariant.Value is CtfStructValue variantStruct))
            {
                throw new LTTngPlaybackException("stream.event.header variant 'v' selected field " +
                                                 $"{eventVariant.Identifier} is not a struct");
            }

            return variantStruct;
        }

        private CtfStructValue GetStreamEventHeaderStructure(ICtfEvent ctfEvent)
        {
            var variantValue = this.GetStreamEventHeaderVariant(ctfEvent);
            return GetStreamEventHeaderStructure(variantValue);
        }

        /// <summary>
        /// The event header has a variant 'v' that contains one of two structs:
        /// "compact" or "extended".
        ///
        /// In the compact case, the variant tag is used as the event id.
        /// In the extended case, the extended structure contains an id field.
        /// </summary>
        /// <param name="ctfEvent">An event</param>
        /// <returns>The id of the event</returns>
        private uint GetEventId(ICtfEvent ctfEvent)
        {
            var eventHeaderVariant = this.GetStreamEventHeaderVariant(ctfEvent);
            Debug.Assert(eventHeaderVariant != null);

            var eventHeaderStruct = GetStreamEventHeaderStructure(eventHeaderVariant);
            Debug.Assert(eventHeaderStruct != null);

            uint id = 0;

            if (StringComparer.InvariantCulture.Equals(eventHeaderStruct.FieldName, "compact"))
            {
                if (!eventHeaderVariant.TagEnum.IntegerValue.TryGetUInt32(out id))
                {
                    throw new LTTngPlaybackException("stream.event.header.v<tag> - tag value is not a uint.");
                }

                return id;
            }

            if (!eventHeaderStruct.FieldsByName.TryGetValue("id", out var idValue))
            {
                throw new LTTngPlaybackException("stream.event.header.v value is not 'id'.");
            }

            if (!(idValue is CtfIntegerValue idFieldIntegerValue))
            {
                throw new LTTngPlaybackException("stream.event.header 'id' field is not an integer.");
            }

            if (!idFieldIntegerValue.Value.TryGetUInt32(out id))
            {
                throw new LTTngPlaybackException($"stream.event.header.v 'id' is not a uint.");
            }

            return id;
        }
    }
}