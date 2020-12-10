// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;
using CtfPlayback.Helpers;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.TypeInterfaces;
using CtfPlayback.Metadata.Types;
using System.Diagnostics;

namespace CtfPlayback.Metadata.NamedScopes
{
    internal class CtfFieldDescriptor
        : ICtfFieldDescriptor
    {
        internal CtfFieldDescriptor(CtfMetadataTypeDescriptor type, string name)
        {
            Debug.Assert(type != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(name));

            this.TypeDescriptor = type;
            this.Name = name;
        }

        /// <inheritdoc />
        public ICtfTypeDescriptor TypeDescriptor { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.TypeDescriptor.ToString() + " " + this.Name;
        }

        /// <inheritdoc />
        public CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null)
        {
            Guard.NotNull(reader, nameof(reader));

            var fieldValue = this.TypeDescriptor.Read(reader, parent);
            fieldValue.FieldName = this.Name;
            return fieldValue;
        }
    }
}