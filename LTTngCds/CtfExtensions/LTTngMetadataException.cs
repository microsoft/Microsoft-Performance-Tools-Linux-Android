// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using CtfPlayback.Metadata;

namespace LTTngCds.CtfExtensions
{
    /// <summary>
    /// An LTTng exception related to trace metadata
    /// </summary>
    public class LTTngMetadataException 
        : CtfMetadataException
    {
        public LTTngMetadataException()
        {
        }

        public LTTngMetadataException(string message) 
            : base("LTTng: " + message)
        {
        }

        public LTTngMetadataException(string message, Exception innerException) 
            : base("LTTng: " + message, innerException)
        {
        }

        protected LTTngMetadataException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}