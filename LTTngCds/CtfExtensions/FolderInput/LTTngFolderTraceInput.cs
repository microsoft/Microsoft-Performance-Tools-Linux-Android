// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CtfPlayback.Inputs;

namespace LTTngCds.CtfExtensions.FolderInput
{
    internal sealed class LTTngFolderTraceInput
        : ICtfTraceInput
    {
        public ICtfInputStream MetadataStream { get; set; }

        public IList<ICtfInputStream> EventStreams { get; set; }

        public void Dispose()
        {
            this.MetadataStream?.Dispose();

            if (EventStreams is null)
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