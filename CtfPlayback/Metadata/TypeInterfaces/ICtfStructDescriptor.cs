// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.Metadata.TypeInterfaces
{
    /// <summary>
    /// Describes a CTF struct type
    /// </summary>
    public interface ICtfStructDescriptor
        : ICtfTypeDescriptor
    {
        /// <summary>
        /// Describes the fields in the struct.
        /// </summary>
        IReadOnlyList<ICtfFieldDescriptor> Fields { get; }

        /// <summary>
        /// Returns a field type descriptor given the field name.
        /// </summary>
        /// <param name="name">Field name</param>
        /// <returns>Field type descriptor</returns>
        ICtfFieldDescriptor GetField(string name);
    }
}