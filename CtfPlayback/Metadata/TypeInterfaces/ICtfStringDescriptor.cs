// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CtfPlayback.Metadata.TypeInterfaces
{
    /// <summary>
    /// Describes a string type
    /// </summary>
    public interface ICtfStringDescriptor
    {
        /// <summary>
        /// The encoding method used for the string.
        /// </summary>
        EncodingTypes Encoding { get; }
    }
}