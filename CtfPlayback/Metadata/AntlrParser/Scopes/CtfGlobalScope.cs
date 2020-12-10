// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.AntlrParser.Scopes
{
    /// <summary>
    /// The root-level CTF metadata scope.
    /// This keeps track of all known CTF sub-scopes, such as 'trace', 'env', 'clock', 'stream', etc.
    /// This maintains a list of all events described in the metadata file.
    /// </summary>
    internal class CtfGlobalScope 
        : CtfScope,
          ICtfMetadataBuilder
    {
        private readonly ICtfMetadataBuilder metadata;

        internal CtfGlobalScope(ICtfMetadataBuilder metadata)
            : base("Global", null)
        {
            Debug.Assert(metadata != null);
            this.metadata = metadata;
        }

        public ICtfTraceDescriptor TraceDescriptor => this.metadata.TraceDescriptor;

        public ICtfEnvironmentDescriptor EnvironmentDescriptor => this.metadata.EnvironmentDescriptor;

        public IReadOnlyList<ICtfClockDescriptor> Clocks => this.metadata.Clocks;

        public IReadOnlyDictionary<string, ICtfClockDescriptor> ClocksByName => this.metadata.ClocksByName;

        public IReadOnlyList<ICtfStreamDescriptor> Streams => this.metadata.Streams;

        public IReadOnlyList<ICtfEventDescriptor> Events => this.metadata.Events;

        public void SetTraceDescriptor(ICtfTraceDescriptor traceDescriptor)
        {
            Debug.Assert(traceDescriptor != null);

            this.metadata.SetTraceDescriptor(traceDescriptor);
        }

        public void SetEnvironmentDescriptor(ICtfEnvironmentDescriptor environmentDescriptor)
        {
            Debug.Assert(environmentDescriptor != null);

            this.metadata.SetEnvironmentDescriptor(environmentDescriptor);
        }

        public void AddEvent(
            IReadOnlyDictionary<string, string> assignments,
            IReadOnlyDictionary<string, ICtfTypeDescriptor> typeDeclarations)
        {
            Debug.Assert(assignments != null);
            Debug.Assert(typeDeclarations != null);

            this.metadata.AddEvent(assignments, typeDeclarations);
        }

        public void AddClock(ICtfClockDescriptor clockDescriptor)
        {
            Debug.Assert(clockDescriptor != null);

            this.metadata.AddClock(clockDescriptor);
        }

        public void AddStream(ICtfStreamDescriptor streamDescriptor)
        {
            Debug.Assert(streamDescriptor != null);

            this.metadata.AddStream(streamDescriptor);
        }
    }
}