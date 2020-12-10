// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CtfPlayback.Inputs;

namespace PerfCds.CtfExtensions.FolderInput
{
    internal sealed class PerfCTFFolderTraceInput
        : ICtfTraceInput
    {
        public ICtfInputStream MetadataStream { get; set; }

        public IList<ICtfInputStream> EventStreams { get; set; }

        public void Dispose()
        {
            this.MetadataStream?.Dispose();

            if (this.EventStreams != null)
            {
                foreach (var eventStream in this.EventStreams)
                {
                    eventStream.Dispose();
                }
            }
        }
    }
}