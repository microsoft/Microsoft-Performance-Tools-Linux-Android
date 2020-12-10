// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfPlayback.Metadata.InternalHelpers
{
    internal class CtfIntegerRange
        : ICtfIntegerRange
    {
        internal CtfIntegerRange(ICtfIntegerDescriptor rangeBase, IntegerLiteral begin, IntegerLiteral end)
        {
            Debug.Assert(rangeBase != null);
            Debug.Assert(begin != null);
            Debug.Assert(end != null);

            this.Base = rangeBase;
            this.Begin = begin;
            this.End = end;
        }

        /// <inheritdoc />
        public ICtfIntegerDescriptor Base { get; }

        /// <inheritdoc />
        public IntegerLiteral Begin { get; }

        /// <inheritdoc />
        public IntegerLiteral End { get; }

        /// <inheritdoc />
        public bool ContainsValue(long value)
        {
            if (!this.Base.Signed)
            {
                if (value < 0)
                {
                    return false;
                }

                if ((ulong)value >= this.Begin.ValueAsUlong)
                {
                    if ((ulong)value <= this.End.ValueAsUlong)
                    {
                        return true;
                    }
                }

                return false;
            }

            if (value >= this.Begin.ValueAsLong)
            {
                if (value <= this.End.ValueAsLong)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool ContainsValue(ulong value)
        {
            if (this.Base.Signed)
            {
                if ((long)value >= this.Begin.ValueAsLong)
                {
                    if ((long)value <= this.End.ValueAsLong)
                    {
                        return true;
                    }
                }

                return false;
            }

            if (value >= this.Begin.ValueAsUlong)
            {
                if (value <= this.End.ValueAsUlong)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (StringComparer.CurrentCulture.Equals(this.Begin, this.End))
            {
                return $"{this.Begin}";
            }

            return $"{this.Begin} ... {this.End}";
        }
    }
}