// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Types
{
    internal abstract class CtfMetadataTypeDescriptor
        : ICtfTypeDescriptor
    {
        protected CtfMetadataTypeDescriptor(CtfTypes type)
        {
            this.CtfType = type;
        }

        /// <inheritdoc />
        public abstract int Align { get; }

        /// <inheritdoc />
        public CtfTypes CtfType { get; protected set; }

        /// <inheritdoc />
        public abstract CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null);
    }
}
