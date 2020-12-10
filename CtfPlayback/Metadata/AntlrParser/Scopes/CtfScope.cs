// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using CtfPlayback.Metadata.InternalHelpers;

namespace CtfPlayback.Metadata.AntlrParser.Scopes
{
    /// <summary>
    /// A basic CTF scope.
    /// Useful for keeping track of information associated with a given scope, including:
    ///     - types declared in the scope
    ///     - named properties declared in the scope
    ///     - the scope name
    ///     - the scope parent
    ///     - the scope children
    /// </summary>
    internal class CtfScope
    {
        private readonly Dictionary<string, TypeDeclaration> types = new Dictionary<string, TypeDeclaration>(StringComparer.InvariantCulture);

        internal CtfScope(string name, CtfScope parent)
        {
            this.Name = name;
            this.Parent = parent;
            this.Children = new Dictionary<string, CtfScope>(StringComparer.InvariantCulture);
            this.PropertyBag = new CtfPropertyBag();
        }

        internal string Name { get; set; }

        internal CtfScope Parent { get; set; }

        internal Dictionary<string, CtfScope> Children { get; }

        internal IReadOnlyDictionary<string, TypeDeclaration> Types => this.types;

        internal CtfPropertyBag PropertyBag { get; }

        internal void AddType(TypeDeclaration scopeType)
        {
            this.types.Add(scopeType.ToString(), scopeType);
        }

        /// <summary>
        /// Searches through this scope, and its ancestors, trying to find the given type.
        /// </summary>
        /// <param name="typeName">The name of the type to find</param>
        /// <returns>The type declaration, or null if not found</returns>
        internal TypeDeclaration FindTypeByName(string typeName)
        {
            if (this.types.TryGetValue(typeName, out var typeDeclaration))
            {
                return typeDeclaration;
            }
            else if (this.Parent != null)
            {
                return this.Parent.FindTypeByName(typeName);
            }

            return null;
        }
    }
}