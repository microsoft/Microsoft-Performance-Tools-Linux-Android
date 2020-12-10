// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Interfaces
{
    public interface ICtfMetadataBuilder
        : ICtfMetadata
    { 
        /// <summary>
        /// Sets the 'trace' scope ing the metadata file.
        /// </summary>
        /// <param name="traceDescriptor">The 'trace' descriptor</param>
        /// <returns>true on success</returns>
        void SetTraceDescriptor(ICtfTraceDescriptor traceDescriptor);

        /// <summary>
        /// Sets the 'env' scope in the metadata file.
        /// </summary>
        /// <param name="environmentDescriptor">The 'env' descriptor</param>
        /// <returns>true on success</returns>
        void SetEnvironmentDescriptor(ICtfEnvironmentDescriptor environmentDescriptor);

        /// <summary>
        /// Add an event descriptor found in the metadata file.
        /// </summary>
        /// <param name="assignments">Type assignments</param>
        /// <param name="typeDeclarations">Type declarations</param>
        void AddEvent(
            IReadOnlyDictionary<string, string> assignments,
            IReadOnlyDictionary<string, ICtfTypeDescriptor> typeDeclarations);

        /// <summary>
        /// Add a clock descriptor found in the metadata file.
        /// </summary>
        /// <param name="clockDescriptor">Clock descriptor</param>
        void AddClock(ICtfClockDescriptor clockDescriptor);

        /// <summary>
        /// Add a stream descriptor found in the metadata file.
        /// </summary>
        /// <param name="streamDescriptor">Stream descriptor</param>
        void AddStream(ICtfStreamDescriptor streamDescriptor);
    }
}