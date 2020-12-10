// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using CtfPlayback.Metadata;

namespace PerfCds.CtfExtensions
{
    /// <summary>
    /// An Perf exception related to trace metadata
    /// </summary>
    public class PerfMetadataException 
        : CtfMetadataException
    {
        public PerfMetadataException()
        {
        }

        public PerfMetadataException(string message) 
            : base("Perf: " + message)
        {
        }

        public PerfMetadataException(string message, Exception innerException) 
            : base("Perf: " + message, innerException)
        {
        }

        protected PerfMetadataException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}