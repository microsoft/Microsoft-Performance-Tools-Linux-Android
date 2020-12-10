// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;
using CtfPlayback.Metadata;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.TypeInterfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CtfUnitTest
{
    public class TestCtfMetadataCustomization
        : ICtfMetadataCustomization,
          ICtfMetadataBuilder
    {
        private readonly List<ICtfStreamDescriptor> streams = new List<ICtfStreamDescriptor>();
        private readonly List<ICtfClockDescriptor> clocks = new List<ICtfClockDescriptor>();
        private readonly List<ICtfEventDescriptor> events = new List<ICtfEventDescriptor>();
        private readonly Dictionary<uint, ICtfEventDescriptor> eventsById = new Dictionary<uint, ICtfEventDescriptor>();

        public ICtfMetadata Metadata => this;

        public string GetTimestampClockName(CtfIntegerValue timestampField)
        {
            throw new NotImplementedException();
        }

        public ICtfTraceDescriptor TraceDescriptor { get; private set; }

        public ICtfEnvironmentDescriptor EnvironmentDescriptor { get; private set; }

        public IReadOnlyList<ICtfClockDescriptor> Clocks => this.clocks;

        public IReadOnlyDictionary<string, ICtfClockDescriptor> ClocksByName { get; set; }

        public IReadOnlyList<ICtfStreamDescriptor> Streams => this.streams;

        public IReadOnlyList<ICtfEventDescriptor> Events => this.events;

        public IReadOnlyDictionary<uint, ICtfEventDescriptor> EventByEventId => this.eventsById;

        public void SetTraceDescriptor(ICtfTraceDescriptor traceDescriptor)
        {
            Assert.IsNull(this.TraceDescriptor);

            this.TraceDescriptor = traceDescriptor;
        }

        public void SetEnvironmentDescriptor(ICtfEnvironmentDescriptor environmentDescriptor)
        {
            Assert.IsNull(this.EnvironmentDescriptor);

            this.EnvironmentDescriptor = environmentDescriptor;
        }

        public void AddEvent(
            IReadOnlyDictionary<string, string> assignments,
            IReadOnlyDictionary<string, ICtfTypeDescriptor> typeDeclarations)
        {
            var newEvent = new TestCtfEventDescriptor
            {
                Assignments = assignments,
                TypeDeclarations = typeDeclarations
            };

            this.events.Add(newEvent);
            this.eventsById.Add(newEvent.Id, newEvent);
        }

        public void AddClock(ICtfClockDescriptor clockDescriptor)
        {
            this.clocks.Add(clockDescriptor);
        }

        public void AddStream(ICtfStreamDescriptor streamDescriptor)
        {
            this.streams.Add(streamDescriptor);
        }
    }
}