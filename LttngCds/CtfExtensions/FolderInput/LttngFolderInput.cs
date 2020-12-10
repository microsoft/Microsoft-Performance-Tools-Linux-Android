// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CtfPlayback.Inputs;

namespace LttngCds.CtfExtensions.FolderInput
{
    internal sealed class LttngFolderInput
        : ICtfInput
    {
        private readonly List<ICtfTraceInput> traces = new List<ICtfTraceInput>();

        public LttngFolderInput(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new ArgumentException("Folder does not exist.", nameof(folderPath));
            }

            var metadataFiles = Directory.GetFiles(folderPath, "metadata", SearchOption.AllDirectories);
            foreach (var metadataFile in metadataFiles)
            {
                // each CTF trace is associated with one metadata stream and one or more event streams.
                var traceInput = new LttngFolderTraceInput
                {
                    MetadataStream = new LttngFileInputStream(metadataFile)
                };

                string traceDirectoryPath = Path.GetDirectoryName(metadataFile);
                Debug.Assert(traceDirectoryPath != null, nameof(traceDirectoryPath) + " != null");

                var associatedEntries = Directory.GetFiles(traceDirectoryPath).Where(entry =>
                    Path.GetFileName(entry).StartsWith("channel"));

                traceInput.EventStreams = associatedEntries.Select(
                    fileName => new LttngFileInputStream(fileName)).Cast<ICtfInputStream>().ToList();

                this.traces.Add(traceInput);
            }
        }

        public IReadOnlyList<ICtfTraceInput> Traces => this.traces;

        public void Dispose()
        {
            foreach (var traceInput in this.traces)
            {
                traceInput.Dispose();
            }
        }
    }
}