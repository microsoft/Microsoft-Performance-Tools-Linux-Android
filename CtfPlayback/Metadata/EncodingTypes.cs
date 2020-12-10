// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CtfPlayback.Metadata
{
    /// <summary>
    /// Text encoding methods
    /// </summary>
    public enum EncodingTypes
    {
        /// <summary>
        /// The encoding type is not specified
        /// </summary>
        None,

        /// <summary>
        /// Uses UTF-8 encoding
        /// </summary>
        Utf8,

        /// <summary>
        /// Uses ASCII encoding
        /// </summary>
        Ascii,

        /// <summary>
        /// The encoding type was specified and is not valid
        /// </summary>
        Invalid
    }
}