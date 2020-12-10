// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace CtfPlayback.Metadata
{
    /// <summary>
    /// Exception thrown because of problems with the CTF metadata.
    /// </summary>
    public class CtfMetadataException 
        : Exception
    {
        public CtfMetadataException()
        {
        }

        public CtfMetadataException(string message) : base(message)
        {
        }

        public CtfMetadataException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CtfMetadataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}