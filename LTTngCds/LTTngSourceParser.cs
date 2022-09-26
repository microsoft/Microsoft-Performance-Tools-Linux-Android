// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using CtfPlayback;
using CtfPlayback.Inputs;
using LTTngCds.CookerData;
using LTTngCds.CtfExtensions;
using LTTngCds.CtfExtensions.FolderInput;
using LTTngCds.CtfExtensions.ZipArchiveInput;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;
using ILogger = Microsoft.Performance.SDK.Processing.ILogger;

namespace LTTngCds
{
    internal sealed class LTTngSourceParser
        : SourceParser<LTTngEvent, LTTngContext, string>,
          IDisposable
    {
        private ICtfInput ctfInput;
        private DataSourceInfo dataSourceInfo;
        private readonly long timeOffsetNanos = 0;

        public LTTngSourceParser(ProcessorOptions options)
        {
            IEnumerable<String> timeOffsetArgs;
            if (options.Options.TryGetOptionArguments("LTTngOffsetTime", out timeOffsetArgs))
            {
                long localOffset = 0;
                if (long.TryParse(timeOffsetArgs.First(), out localOffset))
                {
                    timeOffsetNanos = localOffset;
                }
            }
        }

        public void SetZippedInput(string pathToZip)
        {
            try
            {
                var zipArchive = ZipFile.Open(pathToZip, ZipArchiveMode.Read);
                this.ctfInput = new LTTngZipArchiveInput(zipArchive);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to open LTTng zip archive: {e.Message}");
                throw;
            }
        }

        public void SetFolderInput(string folderPath)
        {
            try
            {
                this.ctfInput = new LTTngFolderInput(folderPath);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to open LTTng folder: {e.Message}");
                throw;
            }
        }

        public override string Id => LTTngConstants.SourceId;

        public ulong EventCount { get; private set; }

        public Timestamp FirstEventTimestamp { get; private set; }

        public Timestamp LastEventTimestamp { get; private set; }

        public DateTime FirstEventWallClock { get; private set; }

        public ulong ProcessingTimeInMilliseconds { get; private set; }

        public override DataSourceInfo DataSourceInfo => this.dataSourceInfo;

        internal Dictionary<string, TraceStatsData> TraceStats = new Dictionary<string, TraceStatsData>(StringComparer.Ordinal);

        public override void ProcessSource(
            ISourceDataProcessor<LTTngEvent, LTTngContext, string> dataProcessor,
            ILogger logger,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            Progress<byte> progressReport = new Progress<byte>((percentComplete) => progress.Report(percentComplete));

            void EventCallback(LTTngEvent lttngEvent, LTTngContext lttngContext)
            {
                if (timeOffsetNanos != 0)
                {
                    lttngEvent = new LTTngEvent(lttngEvent, timeOffsetNanos);
                }

                if (this.EventCount == 0)
                {
                    this.FirstEventTimestamp = lttngEvent.Timestamp;
                    this.FirstEventWallClock = lttngEvent.WallClockTime;
                }
                else
                {
                    this.LastEventTimestamp = lttngEvent.Timestamp;
                }

                EventCount++;

                dataProcessor.ProcessDataElement(lttngEvent, lttngContext, cancellationToken);

                if (!this.TraceStats.TryGetValue(lttngEvent.Name, out var traceStats))
                {
                    traceStats = new TraceStatsData();
                    this.TraceStats.Add(lttngEvent.Name, traceStats);
                }

                traceStats.EventCount++;
                traceStats.PayloadBitCount += lttngEvent.PayloadBitCount;
            }

            {
                var sw = new Stopwatch();
                sw.Start();

                var lttngCustomization = new LTTngPlaybackCustomization(this.ctfInput);
                lttngCustomization.RegisterEventCallback(EventCallback);

                var playback = new CtfPlayback.CtfPlayback(lttngCustomization, cancellationToken);
                playback.Playback(this.ctfInput, new CtfPlaybackOptions { ReadAhead = true }, progressReport);

                sw.Stop();
                this.ProcessingTimeInMilliseconds = (ulong)sw.ElapsedMilliseconds;
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