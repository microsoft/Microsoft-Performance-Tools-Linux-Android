// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.TypeInterfaces;
using LTTngCds.CtfExtensions.Descriptors;

namespace LTTngCds.CtfExtensions
{
    internal class LTTngMetadata
        : ICtfMetadataBuilder
    {
        private readonly List<ICtfStreamDescriptor> streams = new List<ICtfStreamDescriptor>();
        private readonly List<ICtfClockDescriptor> clocks = new List<ICtfClockDescriptor>();
        private readonly List<ICtfEventDescriptor> events = new List<ICtfEventDescriptor>();
        private readonly Dictionary<uint, ICtfEventDescriptor> eventsById = new Dictionary<uint, ICtfEventDescriptor>();
        private readonly Dictionary<string, ICtfClockDescriptor> clocksByName = new Dictionary<string, ICtfClockDescriptor>();

        public ICtfTraceDescriptor TraceDescriptor { get; private set; }

        public ICtfEnvironmentDescriptor EnvironmentDescriptor { get; private set; }

        public IReadOnlyList<ICtfClockDescriptor> Clocks => this.clocks;

        public IReadOnlyDictionary<string, ICtfClockDescriptor> ClocksByName => this.clocksByName;

        public IReadOnlyList<ICtfStreamDescriptor> Streams => this.streams;

        public IReadOnlyList<ICtfEventDescriptor> Events => this.events;

        public IReadOnlyDictionary<uint, ICtfEventDescriptor> EventByEventId => this.eventsById;

        public void SetTraceDescriptor(ICtfTraceDescriptor traceDescriptor)
        {
            Debug.Assert(traceDescriptor != null);

            this.TraceDescriptor = traceDescriptor;
        }

        public void SetEnvironmentDescriptor(ICtfEnvironmentDescriptor environmentDescriptor)
        {
            Debug.Assert(environmentDescriptor != null);

            this.EnvironmentDescriptor = environmentDescriptor;
        }

        public void AddEvent(
            IReadOnlyDictionary<string, string> assignments,
            IReadOnlyDictionary<string, ICtfTypeDescriptor> typeDeclarations)
        {
            var eventDescriptor = new EventDescriptor(assignments, typeDeclarations);
            this.events.Add(eventDescriptor);
            this.eventsById.Add(eventDescriptor.Id, eventDescriptor);
        }

        public void AddClock(ICtfClockDescriptor clockDescriptor)
        {
            this.clocks.Add(clockDescriptor);
            this.clocksByName.Add(clockDescriptor.Name, clockDescriptor);
        }

        public void AddStream(ICtfStreamDescriptor streamDescriptor)
        {
            this.streams.Add(streamDescriptor);
        }
    }
}