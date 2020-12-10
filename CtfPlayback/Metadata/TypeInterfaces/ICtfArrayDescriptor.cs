// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CtfPlayback.Metadata.TypeInterfaces
{
    /// <summary>
    /// Describes a CTF array
    /// </summary>
    public interface ICtfArrayDescriptor
        : ICtfTypeDescriptor
    {
        /// <summary>
        /// The array element type
        /// </summary>
        ICtfTypeDescriptor Type { get; }

        /// <summary>
        /// The string value of the index
        /// </summary>
        string Index { get; }
    }
}