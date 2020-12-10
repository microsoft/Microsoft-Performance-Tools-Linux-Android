// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CtfPlayback.FieldValues;
using CtfPlayback.Helpers;
using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.Types
{
    internal class CtfEnumDescriptor 
        : CtfMetadataTypeDescriptor, 
          ICtfEnumDescriptor
    {
        private readonly Dictionary<string, CtfNamedRange> valuesByName = new Dictionary<string, CtfNamedRange>();

        // this is used for determining the value of the next enumerator identifier when a value is not specified
        private readonly IntegerLiteral nextDefaultValue;

        internal CtfEnumDescriptor(CtfIntegerDescriptor baseType)
            : base(CtfTypes.Enum)
        {
            Debug.Assert(baseType != null);

            this.BaseType = baseType;

            if (baseType.Signed)
            {
                this.nextDefaultValue = new IntegerLiteral(0L);
            }
            else
            {
                this.nextDefaultValue = new IntegerLiteral(0ul);
            }
        }

        /// <inheritdoc />
        public ICtfIntegerDescriptor BaseType { get; }

        /// <inheritdoc />
        public IEnumerable<ICtfNamedRange> EnumeratorValues => this.valuesByName.Values;

        /// <inheritdoc />
        /// <remarks>
        /// Enumerator alignment comes from the integer base class.
        /// </remarks>
        public override int Align => this.BaseType.Align;

        /// <inheritdoc />
        public string GetName(ulong value)
        {
            foreach (CtfNamedRange range in this.EnumeratorValues)
            {
                if (range.ContainsValue(value))
                {
                    return range.Name;
                }
            }

            throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public string GetName(long value)
        {
            foreach (CtfNamedRange range in this.EnumeratorValues)
            {
                if (range.ContainsValue(value))
                {
                    return range.Name;
                }
            }

            throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public ICtfNamedRange GetValue(string name)
        {
            Guard.NotNullOrWhiteSpace(name, nameof(name));

            return this.EnumeratorValues.SingleOrDefault(p => p.Name == name);
        }

        /// <inheritdoc />
        public override CtfFieldValue Read(IPacketReader reader, CtfFieldValue parent = null)
        {
            Guard.NotNull(reader, nameof(reader));

            var valueAsIntegerValue = this.BaseType.Read(reader, parent);
            if (valueAsIntegerValue == null)
            {
                // todo:we need some sort of error reporting
                return null;
            }

            if (!(valueAsIntegerValue is CtfIntegerValue integerValue))
            {
                return null;
            }

            string enumValue;
            if (integerValue.Value.Signed)
            {
                enumValue = this.GetName(integerValue.Value.ValueAsLong);
            }
            else
            {
                enumValue = this.GetName(integerValue.Value.ValueAsUlong);
            }

            return new CtfEnumValue(enumValue, integerValue.Value);
        }

        internal bool AddRange(string identifierName, CtfIntegerRange range)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(identifierName));
            Debug.Assert(range != null);

            // todo:check for overlapping ranges

            if (!range.Base.Equals(this.BaseType))
            {
                return false;
            }

            if (!this.valuesByName.TryGetValue(identifierName, out var namedRange))
            {
                namedRange = new CtfNamedRange(identifierName);
                this.valuesByName.Add(identifierName, namedRange);
            }

            namedRange.AddRange(range);

            if (this.BaseType.Signed)
            {
                this.nextDefaultValue.UpdateValue(range.End + 1L);
            }
            else
            {
                this.nextDefaultValue.UpdateValue(range.End + 1ul);
            }

            return true;
        }

        internal IntegerLiteral GetNextDefaultValue()
        {
            if (this.nextDefaultValue.RequiredBitCount > this.BaseType.Size)
            {
                throw new OverflowException();
            }

            return this.nextDefaultValue;
        }
    }
}