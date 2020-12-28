// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using CtfPlayback.Inputs;
using CtfPlayback.Metadata.Interfaces;
using LTTngCds.CtfExtensions;

namespace LTTngCds.CookerData
{
    /// <summary>
    /// Provides additional data beyond the event for event processing.
    /// </summary>
    public class LTTngContext
        : ICursor
    {
        private readonly LTTngMetadataCustomization metadata;
        private readonly ICtfInputStream eventStream;
        private readonly TraceContext traceContext;

        internal LTTngContext(LTTngMetadataCustomization metadata, ICtfInputStream eventStream, TraceContext traceContext)
        {
            this.metadata = metadata;
            this.eventStream = eventStream;
            this.traceContext = traceContext;
        }

        /// <summary>
        /// The clocks described in the LTTNG trace.
        /// </summary>
        public IReadOnlyDictionary<string, ICtfClockDescriptor> Clocks => this.metadata.Metadata.ClocksByName;

        /// <inheritdoc />
        public long Timestamp { get; internal set; }

        /// <inheritdoc />
        public uint CurrentCpu { get; internal set; }

        /// <inheritdoc />
        public ulong CurrentEventNumber { get; internal set; }

        /// <inheritdoc />
        public ulong CurrentEventNumberWithinTrace { get; internal set; }

        /// <inheritdoc />
        public string StreamSource => this.eventStream.StreamSource;

        public string HostName => this.traceContext.HostName;

        public string Domain => this.traceContext.Domain;

        public string SysName => this.traceContext.SysName;

        public string KernelRelease => this.traceContext.KernelRelease;

        public string KernelVersion => this.traceContext.KernelVersion;

        public string TracerName => this.traceContext.TracerName;

        public uint TracerMajor => this.traceContext.TracerMajor;

        public uint TracerMinor => this.traceContext.TracerMinor;

        public uint TracerPathLevel => this.traceContext.TracerPathLevel;
    }
}