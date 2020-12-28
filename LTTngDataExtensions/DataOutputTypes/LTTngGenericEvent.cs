// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using CtfPlayback;
using CtfPlayback.FieldValues;
using LTTngCds.CookerData;
using Microsoft.Performance.SDK;

namespace LTTngDataExtensions.DataOutputTypes
{
    public class EventKind
    {
        public static readonly Dictionary<uint, EventKind> RegisteredKinds = new Dictionary<uint, EventKind>();
        public uint Id { get; }
        public string EventName { get; }
        public readonly List<string> FieldNames;
        public EventKind(uint id, string name, IReadOnlyList<CtfFieldValue> fields)
        {
            this.Id = id;
            this.EventName = name;
            this.FieldNames = new List<string>(fields.Count);
            foreach (var field in fields)
            {
                this.FieldNames.Add(field.FieldName);
            }
        }
    }

    public struct LTTngGenericEvent
    {
        private readonly EventKind kind;

        public LTTngGenericEvent(LTTngEvent data, LTTngContext context)
        {
      
            if (!(data.Payload is CtfStructValue payload))
            {
                throw new CtfPlaybackException("Event data is corrupt.");
            }

            this.Timestamp = data.Timestamp;
            this.CpuId = context.CurrentCpu;
            this.DiscardedEvents = data.DiscardedEvents;

            if (!EventKind.RegisteredKinds.TryGetValue(data.Id, out this.kind))
            {
                this.kind = new EventKind(data.Id, data.Name, payload.Fields);
                EventKind.RegisteredKinds.Add(data.Id, this.kind);
            }

            // As this is being written, all columns are of type 'T', so all rows are the same. For generic events,
            // where columns have different types for different rows, this means everything becomes a string.
            //
            // We don't want to keep around each event in memory, that would use too much memory. So for now convert
            // each field value to a string, which would happen anyways.
            //
            // If the consumer is smarter in the future and allows for multi-type columns, we can re-evaluate this
            // approach. We could probably generate a type from each event descriptor, and convert to that type.
            //

            this.FieldValues = new List<string>(payload.Fields.Count);
            foreach (var field in payload.Fields)
            {
                this.FieldValues.Add(field.GetValueAsString());
            }
        }

        public string EventName => this.kind.EventName;

        public Timestamp Timestamp { get; }

        public uint Id => this.kind.Id;

        public uint CpuId { get; }

        public uint DiscardedEvents { get; }

        public readonly List<string> FieldValues;
        public List<string> FieldNames => this.kind.FieldNames;
    }
}