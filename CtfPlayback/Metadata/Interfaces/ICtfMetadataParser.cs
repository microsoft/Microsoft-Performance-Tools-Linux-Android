// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

namespace CtfPlayback.Metadata.Interfaces
{
    /// <summary>
    /// Metadata parser interface
    /// </summary>
    public interface ICtfMetadataParser
    {
        /// <summary>
        /// Parses a CTF metadata file.
        /// </summary>
        /// <param name="metadataStream">Metadata stream</param>
        /// <returns>The parsed metadata</returns>
        ICtfMetadata Parse(Stream metadataStream);
    }
}