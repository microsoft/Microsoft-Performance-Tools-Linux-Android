// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CtfPlayback;
using CtfPlayback.FieldValues;
using PerfCds.CookerData;
using Microsoft.Performance.SDK;

namespace PerfDataExtensions.DataOutputTypes
{
    public struct PerfGenericEventField
    {
        public PerfGenericEventField(string name, string value)
        {
            Name = string.Intern(name);
            Value = string.Intern(value);
        }

        public string Name { get; }

        public string Value { get; }
    }

    public struct PerfGenericEvent
    {
        private readonly List<PerfGenericEventField> events;

        public PerfGenericEvent(PerfEvent data, PerfContext context)
        {
            this.EventName = data.Name;
            this.Timestamp = data.Timestamp;
            this.Id = data.Id;
            this.CpuId = context.CurrentCpu;

            if (!(data.Payload is CtfStructValue payload))
            {
                throw new CtfPlaybackException("Event data is corrupt.");
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

            this.FieldCount = payload.Fields.Count;
            this.events = new List<PerfGenericEventField>(this.FieldCount);
            foreach (var field in payload.Fields)
            {
                this.events.Add(new PerfGenericEventField(field.FieldName, field.GetValueAsString()));
            }
        }

        public string EventName { get; }

        public Timestamp Timestamp { get; }

        public uint Id { get; }

        public uint CpuId { get; }

        public int FieldCount { get; }

        public PerfGenericEventField this[int fieldIndex] => this.events[fieldIndex];
    }
}