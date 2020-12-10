// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.EventStreams;
using CtfPlayback.Helpers;
using CtfPlayback.Inputs;
using CtfPlayback.Metadata.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CtfPlayback
{
    /// <summary>
    /// This class is responsible for iterating through all events of all event streams, playing them back in time order.
    /// </summary>
    public class CtfPlayback
    {
        private readonly ICtfPlaybackCustomization customization;
        private readonly CancellationToken cancellationToken;

        private ulong totalBytesToProcess;

        private ulong bytesProcessedFromCompletedStreams;

        private ulong eventCount;

        // Calling DateTime.Now for each event would slow down event processing
        // So instead, try updating the progress every N events.
        private ulong lastProgressUpdateTimeEventNumber;

        private ulong lastEventTimestamp;

        private Dictionary<CtfStreamPlayback, ICtfTraceInput> streamToTrace = new Dictionary<CtfStreamPlayback, ICtfTraceInput>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="customization">Playback customization</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public CtfPlayback(
            ICtfPlaybackCustomization customization,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(customization, nameof(customization));
            Guard.NotNull(cancellationToken, nameof(cancellationToken));

            this.customization = customization;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Playback the events in time order. Event callbacks happen through ICtfPlaybackCustomization.ProcessEvent,
        /// allowing for customized event delivery.
        /// </summary>
        /// <param name="source">CTF trace input source</param>
        /// <param name="playbackOptions">Playback options</param>
        /// <param name="progress">Progress meter</param>
        public void Playback(
            ICtfInput source,
            CtfPlaybackOptions playbackOptions,
            IProgress<byte> progress)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(progress, nameof(progress));

            var playbackStreams = GeneratePlaybackStreams(source, playbackOptions);

            while (playbackStreams.Any())
            {
                // Find the event stream with the earlieste available event.

                CtfStreamPlayback streamPlayback = playbackStreams[0];
                for (int x = 1; x < playbackStreams.Count; x++)
                {
                    if (playbackStreams[x].CurrentEvent.Timestamp.NanosecondsFromPosixEpoch < streamPlayback.CurrentEvent.Timestamp.NanosecondsFromPosixEpoch)
                    {
                        streamPlayback = playbackStreams[x];
                    }
                }

                if (streamPlayback.CurrentEvent.Timestamp.NanosecondsFromPosixEpoch < this.lastEventTimestamp)
                {
                    Debug.Assert(false, "time inversion?");
                    Console.Error.WriteLine("Time inversion discovered in Lttng trace.");
                }

                this.customization.ProcessEvent(
                    streamPlayback.CurrentEvent, 
                    streamPlayback.CurrentPacket, 
                    this.streamToTrace[streamPlayback], 
                    streamPlayback.EventStream);

                this.eventCount++;

                this.lastEventTimestamp = streamPlayback.CurrentEvent.Timestamp.NanosecondsFromPosixEpoch;

                if (!streamPlayback.MoveToNextEvent())
                {
                    playbackStreams.Remove(streamPlayback);
                    this.bytesProcessedFromCompletedStreams += streamPlayback.EventStream.ByteCount;
                }

                if (this.eventCount - this.lastProgressUpdateTimeEventNumber >= 5000)
                {
                    this.UpdateProgress(playbackStreams, progress);
                }

                this.cancellationToken.ThrowIfCancellationRequested();
            }

            this.UpdateProgress(playbackStreams, progress);
        }

        private void UpdateProgress(IList<CtfStreamPlayback> playbackStreams, IProgress<byte> progress)
        {
            ulong bytesProcessedFromActiveStreams = 0;
            foreach (var activeStream in playbackStreams)
            {
                bytesProcessedFromActiveStreams += activeStream.CountOfBytesProcessed;
            }

            ulong totalBytesProcessed = this.bytesProcessedFromCompletedStreams + bytesProcessedFromActiveStreams;

            var completed = (double)totalBytesProcessed / this.totalBytesToProcess * 100;
            progress.Report((byte)Math.Truncate(completed));

            this.lastProgressUpdateTimeEventNumber = this.eventCount;
        }

        private IList<CtfStreamPlayback> GeneratePlaybackStreams(ICtfInput source, CtfPlaybackOptions playbackOptions)
        {
            var playbackStreams = new List<CtfStreamPlayback>();

            // Initialize packets from all streams, and sort by times
            foreach (var trace in source.Traces)
            {
                var metadataParser = customization.CreateMetadataParser(trace);
                ICtfMetadata metadata = metadataParser.Parse(trace.MetadataStream.Stream);

                for (int streamIndex = 0; streamIndex < trace.EventStreams.Count; streamIndex++)
                {
                    var stream = trace.EventStreams[streamIndex];
                    var eventStream = new CtfEventStream(stream, metadata, customization);
                    var streamPlayback = new CtfStreamPlayback(eventStream, playbackOptions, cancellationToken);
                    if (eventStream.ByteCount > 0 && streamPlayback.MoveToNextEvent())
                    {
                        Debug.Assert(streamPlayback.CurrentEvent != null);
                        playbackStreams.Add(streamPlayback);
                        this.streamToTrace.Add(streamPlayback, trace);
                        this.totalBytesToProcess += stream.ByteCount;
                    }
                    else
                    {
                        Debug.Assert(false, eventStream.StreamSource + " appears to have no data.\n\n Ignoring the error will cause the trace to be partially loaded.");
                    }
                }
            }

            return playbackStreams;
        }
    }
}