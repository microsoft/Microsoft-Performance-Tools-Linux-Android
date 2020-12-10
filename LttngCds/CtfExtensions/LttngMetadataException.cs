// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using CtfPlayback.Metadata;

namespace LttngCds.CtfExtensions
{
    /// <summary>
    /// An LTTNG exception related to trace metadata
    /// </summary>
    public class LttngMetadataException 
        : CtfMetadataException
    {
        public LttngMetadataException()
        {
        }

        public LttngMetadataException(string message) 
            : base("LTTNG: " + message)
        {
        }

        public LttngMetadataException(string message, Exception innerException) 
            : base("LTTNG: " + message, innerException)
        {
        }

        protected LttngMetadataException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}