// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using CtfPlayback.Metadata;

namespace LTTngCds.CtfExtensions
{
    /// <summary>
    /// An LTTNG exception related to trace metadata
    /// </summary>
    public class LTTngMetadataException 
        : CtfMetadataException
    {
        public LTTngMetadataException()
        {
        }

        public LTTngMetadataException(string message) 
            : base("LTTNG: " + message)
        {
        }

        public LTTngMetadataException(string message, Exception innerException) 
            : base("LTTNG: " + message, innerException)
        {
        }

        protected LTTngMetadataException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}