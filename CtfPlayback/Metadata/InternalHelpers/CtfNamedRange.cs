// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CtfPlayback.Metadata.InternalHelpers
{
    /// <summary>
    /// There's nothing in the spec that prevents an enum value identifier from having two different value
    /// ranges.
    /// </summary>
    internal class CtfNamedRange
        : ICtfNamedRange
    {
        private readonly List<ICtfIntegerRange> ranges;

        internal CtfNamedRange(string name)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(name));

            this.ranges = new List<ICtfIntegerRange>();
            this.Name = name;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public IReadOnlyList<ICtfIntegerRange> Ranges => this.ranges;

        // todo: should we make a ContainsValue that takes an IntegerLiteral? I think it makes sense, but not high enough priority to do it now...

        /// <inheritdoc />
        public bool ContainsValue(long value)
        {
            foreach (var range in this.Ranges)
            {
                if (range.ContainsValue(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool ContainsValue(ulong value)
        {
            foreach (var range in this.Ranges)
            {
                if (range.ContainsValue(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var sb = new StringBuilder($"{this.Name} = ");

            if (!this.Ranges.Any())
            {
                sb.Append("<error: no range>");
                return sb.ToString();
            }

            sb.Append(this.Ranges[0].ToString());

            for (int x = 1; x < this.Ranges.Count; x++)
            {
                sb.Append(", ");
                sb.Append(this.Ranges[x].ToString());
            }

            return sb.ToString();
        }

        internal bool AddRange(CtfIntegerRange range)
        {
            Debug.Assert(range != null);

            this.ranges.Add(range);
            return true;
        }
    }
}