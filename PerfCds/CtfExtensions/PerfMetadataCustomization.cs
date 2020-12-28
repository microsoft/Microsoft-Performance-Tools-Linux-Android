// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using CtfPlayback.FieldValues;
using CtfPlayback.Inputs;
using CtfPlayback.Metadata;
using CtfPlayback.Metadata.Interfaces;

namespace PerfCds.CtfExtensions
{
    internal class PerfMetadataCustomization
        : ICtfMetadataCustomization
    {
        private readonly List<PerfMetadata> metadata;
        private PerfMetadata currentMetadata;

        public PerfMetadataCustomization(int traceCount)
        {
            this.metadata = new List<PerfMetadata>(traceCount);
        }

        public ICtfMetadata Metadata => this.currentMetadata;

        // Check if needed for Perf?
        public string GetTimestampClockName(CtfIntegerValue timestampField)
        {
            if (String.IsNullOrWhiteSpace(timestampField.MapValue))
            {
                return null;
            }

            // note: this may be overly cautious making this LTTng specific, but it's not clear to me that the CTF
            // specification mandates this clock reference format. It might just be another "example".

            // LTTng maps are in the form of: clock.<clock_name>.value
            // where <clock_name> is the name of the clock
            // e.g. "clock.monotonic.value"

            string map = timestampField.MapValue;

            int firstSplitIndex = map.IndexOf('.');
            string clockToken = map.Substring(0, firstSplitIndex);
            if (!StringComparer.Ordinal.Equals(clockToken, "clock"))
            {
                return null;
            }

            int lastSplitIndex = map.LastIndexOf('.');
            string valueToken = map.Substring(lastSplitIndex + 1);
            if (!StringComparer.Ordinal.Equals(valueToken, "value"))
            {
                return null;
            }

            return map.Substring(firstSplitIndex + 1, lastSplitIndex - firstSplitIndex - 1);
        }

        internal PerfMetadata PerfMetadata => this.currentMetadata;

        internal IReadOnlyList<PerfMetadata> GetMetadata => this.metadata;

        internal void PrepareForNewTrace(ICtfInputStream metadataStream)
        {
            this.currentMetadata = new PerfMetadata();
            this.metadata.Add(this.currentMetadata);
        }
    }
}