// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using CtfPlayback.Metadata.Types;

namespace CtfPlayback.Metadata.AntlrParser
{
    internal enum DeclarationMethod
    {
        Unspecified,
        TypeAlias,
        TypeDef,
        TypeAssignment,
        EnumDeclaration,
        StructDeclaration,
        VariantDeclaration
    }

    internal class TypeDeclaration
    {
        public CtfMetadataTypeDescriptor Type { get; set; }

        // This is not a simple string, because this type of syntax is valid
        // typealias integer { size=64; signed=false; } := unsigned long;
        // And now "unsigned long" are tied together. "long" has no meaning outside of "unsigned long".
        //
        public IEnumerable<string> TypeName { get; set; }

        public override string ToString()
        {
            if (this.TypeName == null || !this.TypeName.Any())
            {
                return "n/a";
            }

            return string.Join(" ", this.TypeName);
        }

        // This is for analysis/debuggin only.
        internal DeclarationMethod DeclarationMethod { get; set; }
    }
}
