// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.Metadata
{
    internal static class CtfMetadataExtensions
    {
        /// <summary>
        /// Finds the stream descriptor with the given id. Stream ids are usually also their index, but
        /// this is not required (e.g. CTF 2 metadata may declare streams in any order).
        /// </summary>
        internal static ICtfStreamDescriptor GetStream(this ICtfMetadata metadata, uint streamId)
        {
            var streams = metadata.Streams;
            if (streamId < streams.Count && streams[(int)streamId].Id == streamId)
            {
                return streams[(int)streamId];
            }

            foreach (var stream in streams)
            {
                if (stream.Id == streamId)
                {
                    return stream;
                }
            }

            throw new CtfPlaybackException($"The metadata does not describe stream id {streamId}.");
        }
    }
}
