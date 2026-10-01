// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using CtfPlayback.Metadata.AntlrParser;
using CtfPlayback.Metadata.Ctf2;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.Metadata
{
    /// <summary>
    /// Parses either CTF 1.8 (TSDL) or CTF 2 (JSON) metadata, based on the content of the metadata stream.
    /// </summary>
    public sealed class CtfVersionDetectingMetadataParser
        : ICtfMetadataParser
    {
        private readonly ICtfMetadataCustomization metadataCustomization;
        private readonly ICtfMetadataBuilder metadataBuilder;
        private readonly bool prefixCtf2EventFieldNamesWithUnderscore;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="metadataCustomization">Extension points for parsing metadata</param>
        /// <param name="metadataBuilder">The object used to build metadata</param>
        /// <param name="prefixCtf2EventFieldNamesWithUnderscore">
        /// See <see cref="Ctf2MetadataParser(ICtfMetadataBuilder, bool)"/>.
        /// </param>
        public CtfVersionDetectingMetadataParser(
            ICtfMetadataCustomization metadataCustomization,
            ICtfMetadataBuilder metadataBuilder,
            bool prefixCtf2EventFieldNamesWithUnderscore)
        {
            this.metadataCustomization = metadataCustomization;
            this.metadataBuilder = metadataBuilder ?? throw new ArgumentNullException(nameof(metadataBuilder));
            this.prefixCtf2EventFieldNamesWithUnderscore = prefixCtf2EventFieldNamesWithUnderscore;
        }

        /// <inheritdoc />
        public ICtfMetadata Parse(Stream metadataStream)
        {
            if (metadataStream == null)
            {
                throw new ArgumentNullException(nameof(metadataStream));
            }

            byte[] metadata = CtfMetadataText.ReadAllBytes(metadataStream);
            string text = CtfMetadataText.Read(metadata);

            if (Ctf2MetadataParser.IsCtf2Metadata(text))
            {
                return new Ctf2MetadataParser(this.metadataBuilder, this.prefixCtf2EventFieldNamesWithUnderscore).Parse(text);
            }

            using (var memoryStream = new MemoryStream(metadata, false))
            {
                return new CtfAntlrMetadataParser(this.metadataCustomization, this.metadataBuilder).Parse(memoryStream);
            }
        }
    }
}
