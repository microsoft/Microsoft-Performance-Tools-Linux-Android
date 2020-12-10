// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.Metadata.TypeInterfaces
{
    /// <summary>
    /// Describes a CTF variant type
    /// </summary>
    internal interface ICtfVariantDescriptor
        : ICtfTypeDescriptor
    {
        /// <summary>
        /// The name of the enum field used as a switch for this variant.
        /// </summary>
        string Switch { get; }

        /// <summary>
        /// Descriptions of the possible types in this variant.
        /// </summary>
        IReadOnlyList<ICtfFieldDescriptor> Union { get; }


        /// <summary>
        /// Returns a type descriptor given the name of the variant type.
        /// </summary>
        /// <param name="name">Variant name</param>
        /// <returns>Field type descriptor</returns>
        ICtfFieldDescriptor GetVariant(string name);
    }
}