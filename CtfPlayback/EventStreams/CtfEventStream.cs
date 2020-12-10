// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using CtfPlayback.EventStreams.Interfaces;
using CtfPlayback.Inputs;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.EventStreams
{
    internal sealed class CtfEventStream
        : ICtfEventStream
    {
        public CtfEventStream(
            ICtfInputStream inputStream,
            ICtfMetadata metadata, 
            ICtfPlaybackCustomization playbackCustomization)
        {
            this.InputStream = inputStream;
            this.Metadata = metadata;
            this.PlaybackCustomization = playbackCustomization;
        }

        public ICtfInputStream InputStream { get; }

        public string StreamSource => InputStream.StreamSource;

        public Stream Stream => InputStream.Stream;

        public ICtfMetadata Metadata { get; }

        public ICtfPlaybackCustomization PlaybackCustomization { get; }

        public ulong ByteCount => this.InputStream.ByteCount;

        public void Dispose()
        {
            // this is just a wrapper around an object that owns the input stream
        }
    }
}