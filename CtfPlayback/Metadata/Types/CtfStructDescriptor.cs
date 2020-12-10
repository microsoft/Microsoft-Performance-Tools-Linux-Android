// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.FieldValues;
using CtfPlayback.Helpers;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.TypeInterfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CtfPlayback.Metadata.Types
{
    internal class CtfStructDescriptor 
        : CtfMetadataTypeDescriptor, 
          ICtfStructDescriptor
    {
        private int align;

        /// <summary>
        /// The minimum alignment for a structure is the maximum alignment
        /// of the fields it contains. See specification 1.8.2 section 4.2.1.
        /// </summary>
        private int minimumAlignment;

        internal CtfStructDescriptor(CtfPropertyBag props, ICtfFieldDescriptor[] fields)
            : base(CtfTypes.Struct)
        {
            Guard.NotNull(props, nameof(props));
            Guard.NotNull(fields, nameof(fields));

            int alignment = 1;
            if (props != null)
            {
                alignment = props.GetIntOrNull("align") ?? 1;
            }

            this.Fields = new List<ICtfFieldDescriptor>(fields);

            DetermineMinimumRequiredAlignment(alignment);
        }

        /// <inheritdoc />
        public override int Align => this.align;

        /// <inheritdoc />
        public IReadOnlyList<ICtfFieldDescriptor> Fields { get; private set; }

        /// <inheritdoc />
        public ICtfFieldDescriptor GetField(string name)
        {
            for (int index = 0; index < this.Fields.Count; index++)
            {
                if (this.Fields[index].Name == name)
                {
                    return this.Fields[index];
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null)
        {
            Guard.NotNull(reader, nameof(reader));

            reader.Align((uint)this.Align);

            var structValue = new CtfStructValue();

            foreach (var ctfField in this.Fields)
            {
                CtfFieldValue field = null;
                try
                {
                    field = ctfField.Read(reader, structValue);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }
                bool addedField = structValue.AddValue(field);
                Debug.Assert(addedField);
            }

            return structValue;
        }

        internal void SetAlignmentProperty(int alignment)
        {
            this.align = Math.Max(this.align, alignment);
        }

        private void DetermineMinimumRequiredAlignment(int specifiedAlignment)
        {
            this.minimumAlignment = 1;
            foreach (var field in this.Fields)
            {
                this.minimumAlignment = Math.Max(this.minimumAlignment, field.TypeDescriptor.Align);
            }

            this.align = Math.Max(specifiedAlignment, this.minimumAlignment);
        }
    }
}