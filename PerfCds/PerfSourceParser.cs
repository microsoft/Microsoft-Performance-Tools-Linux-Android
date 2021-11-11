// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Threading;
using CtfPlayback;
using CtfPlayback.Inputs;
using PerfCds.CookerData;
using PerfCds.CtfExtensions;
using PerfCds.CtfExtensions.FolderInput;
using PerfCds.CtfExtensions.ZipArchiveInput;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;
using ILogger = Microsoft.Performance.SDK.Processing.ILogger;

namespace PerfCds
{
    internal sealed class PerfSourceParser
        : SourceParser<PerfEvent, PerfContext, string>,
          IDisposable
    {
        private ICtfInput ctfInput;
        private DataSourceInfo dataSourceInfo;

        public void SetZippedInput(string pathToZip)
        {
            try
            {
                var zipArchive = ZipFile.Open(pathToZip, ZipArchiveMode.Read);
                this.ctfInput = new PerfCtfZipArchiveInput(zipArchive);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to open Perf zip archive: {e.Message}");
                throw;
            }
        }

        public void SetFolderInput(string folderPath)
        {
            try
            {
                this.ctfInput = new PerfCTFFolderInput(folderPath);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to open Perf folder: {e.Message}");
                throw;
            }
        }

        public override string Id => PerfConstants.SourceId;

        public ulong EventCount { get; private set; }

        public Timestamp FirstEventTimestamp { get; private set; }

        public Timestamp LastEventTimestamp { get; private set; }

        public DateTime FirstEventWallClock { get; private set;}

        public ulong ProcessingTimeInMilliseconds { get; private set; }

        public override DataSourceInfo DataSourceInfo => this.dataSourceInfo;

        internal Dictionary<string, TraceStatsData> TraceStats = new Dictionary<string, TraceStatsData>(StringComparer.Ordinal);

        public override void ProcessSource(
            ISourceDataProcessor<PerfEvent, PerfContext, string> dataProcessor, 
            ILogger logger, 
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            Progress<byte> progressReport = new Progress<byte>((percentComplete) => progress.Report(percentComplete));

            void EventCallback(PerfEvent perfEvent, PerfContext perfContext)
            {
                if (this.EventCount == 0)
                {
                    this.FirstEventTimestamp = perfEvent.Timestamp;
                    this.FirstEventWallClock = perfEvent.WallClockTime;
                }
                this.LastEventTimestamp = perfEvent.Timestamp;

                EventCount++;

                dataProcessor.ProcessDataElement(perfEvent, perfContext, cancellationToken);

                if (!this.TraceStats.TryGetValue(perfEvent.Name, out var traceStats))
                {
                    traceStats = new TraceStatsData();
                    this.TraceStats.Add(perfEvent.Name, traceStats);
                }

                traceStats.EventCount++;
                traceStats.PayloadBitCount += perfEvent.PayloadBitCount;
            }

            {
                var sw = new Stopwatch();
                sw.Start();

                var perfCustomization = new PerfPlaybackCustomization(this.ctfInput);
                perfCustomization.RegisterEventCallback(EventCallback);

                var playback = new CtfPlayback.CtfPlayback(perfCustomization, cancellationToken);
                playback.Playback(this.ctfInput, new CtfPlaybackOptions {ReadAhead = true}, progressReport);

                sw.Stop();
                this.ProcessingTimeInMilliseconds = (ulong) sw.ElapsedMilliseconds;
            }

            {
                // the playback object had a lot of memory associated with it. due to some garbage collection concerns
                // (UI delays) and what I read about on msdn
                // (https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/induced),
                // i'm going to try inducing garbage collection here to see if it helps.
                //
                GC.Collect(2, GCCollectionMode.Default, true);
            }

            if (this.EventCount > 0)
            {
                this.dataSourceInfo = new DataSourceInfo(this.FirstEventTimestamp.ToNanoseconds, this.LastEventTimestamp.ToNanoseconds, this.FirstEventWallClock.ToUniversalTime());
            }
            else
            {
                throw new Exception("No events - failure processing .ctf");
            }
        }

        public void Dispose()
        {
            this.ctfInput?.Dispose();
        }
    }
}