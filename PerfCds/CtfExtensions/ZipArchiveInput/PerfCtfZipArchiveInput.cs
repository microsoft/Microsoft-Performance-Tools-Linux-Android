// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CtfPlayback.Inputs;

namespace PerfCds.CtfExtensions.ZipArchiveInput
{
    internal sealed class PerfCtfZipArchiveInput 
        : ICtfInput, 
          IDisposable
    {
        private readonly ZipArchive archive;

        private readonly List<ICtfTraceInput> traces = new List<ICtfTraceInput>();

        public PerfCtfZipArchiveInput(ZipArchive archive)
        {
            this.archive = archive;

            // map each CTF stream in the archive with its metadata
            foreach (ZipArchiveEntry metadataArchive in archive.Entries.Where(archiveEntry => "metadata".Equals(Path.GetFileName(archiveEntry.FullName))))
            {
                // each CTF trace is associated with one metadata stream and one or more event streams.
                var traceInput = new PerfZipArchiveTraceInput
                {
                    MetadataStream = new PerfZipArchiveInputStream(metadataArchive)
                };

                string traceDirectoryPath = Path.GetDirectoryName(metadataArchive.FullName);
                Debug.Assert(traceDirectoryPath != null, nameof(traceDirectoryPath) + " != null");

                this.PointerSize = traceDirectoryPath.EndsWith("64-bit") ? 8 : 4;

                var associatedArchiveEntries = archive.Entries.Where(entry =>
                    Path.GetDirectoryName(entry.FullName) == traceDirectoryPath &&
                     Path.GetFileName(entry.FullName).StartsWith("perf_stream"));

                traceInput.EventStreams = associatedArchiveEntries.Select(
                    archiveEntry => new PerfZipArchiveInputStream(archiveEntry)).Cast<ICtfInputStream>().ToList();

                if (traceInput.EventStreams.Count > 0)
                {
                    traceInput.EstablishNumberOfProcessors();
                    this.NumberOfProc = Math.Max(this.NumberOfProc, traceInput.NumberOfProc);

                    this.traces.Add(traceInput);
                }
            }
        }

        public IReadOnlyList<ICtfTraceInput> Traces => this.traces;

        public int NumberOfProc { get; }

        public int PointerSize { get; }

        public void Dispose()
        {
            this.archive?.Dispose();

            this.traces.ForEach(input => input.Dispose());
        }
    }
}