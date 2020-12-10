// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using CtfPlayback.Inputs;

namespace LttngCds.CtfExtensions.ZipArchiveInput
{
    internal sealed class LttngZipArchiveTraceInput
        : ICtfTraceInput
    {
        public ICtfInputStream MetadataStream { get; set; }

        public IList<ICtfInputStream> EventStreams { get; set; }

        public int NumberOfProc { get; private set; }

        internal void EstablishNumberOfProcessors()
        {
            this.NumberOfProc = (from stream in this.EventStreams
                                    let filename = stream.StreamSource.ToString()
                                    let i = filename.LastIndexOf('_')
                                    let processor = filename.Substring(i + 1)
                                    select int.Parse(processor)
                                ).Max() + 1;
        }

        public void Dispose()
        {
            this.MetadataStream?.Dispose();

            if (this.EventStreams is null)
            {
                return;
            }

            foreach (var eventStream in this.EventStreams)
            {
                eventStream.Dispose();
            }
        }
    }
}