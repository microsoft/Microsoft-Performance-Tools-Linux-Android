// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.Metadata.AntlrParser.Scopes
{
    /// <summary>
    /// This is a scope used for constructs that have multiple child elements, such as structs and variants.
    /// </summary>
    internal class CtfCompoundTypeScope
        : CtfScope
    {
        internal CtfCompoundTypeScope(string name, CtfScope parent)
            :base(name, parent)
        {
        }

        internal List<ICtfFieldDescriptor> Fields { get; } = new List<ICtfFieldDescriptor>();
    }
}