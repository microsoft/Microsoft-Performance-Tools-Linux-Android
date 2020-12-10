// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CtfPlayback.Metadata
{
    /// <summary>
    /// Types defined in the CTF specification.
    /// </summary>
    public enum CtfTypes
    {
        /// <summary>
        /// An unknown CTF descriptor type
        /// </summary>
        Unknown,

        /// <summary>
        /// A string CTF descriptor type
        /// </summary>
        String,

        /// <summary>
        /// An integer CTF descriptor type
        /// </summary>
        Integer,

        /// <summary>
        /// A struct CTF descriptor type
        /// </summary>
        Struct,

        /// <summary>
        /// An array CTF descriptor type
        /// </summary>
        Array,

        /// <summary>
        /// An enum CTF descriptor type
        /// </summary>
        Enum,

        /// <summary>
        /// A variant CTF descriptor type
        /// </summary>
        Variant,

        /// <summary>
        /// A float CTF descriptor type
        /// </summary>
        Float
    }
}