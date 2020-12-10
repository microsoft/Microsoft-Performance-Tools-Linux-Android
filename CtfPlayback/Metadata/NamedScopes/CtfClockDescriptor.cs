// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.InternalHelpers;
using System;
using System.Diagnostics;

namespace CtfPlayback.Metadata.NamedScopes
{
    /// <summary>
    /// A clock definition in the trace.
    /// </summary>
    internal class CtfClockDescriptor
        : ICtfClockDescriptor
    {
        internal CtfClockDescriptor(CtfPropertyBag bag)
        {
            Debug.Assert(bag != null);

            this.Name = bag.GetString("name");

            if (bag.ContainsKey("uuid"))
            {
                string uuid = bag.GetString("uuid").Replace("\"", string.Empty);
                if (!Guid.TryParse(uuid, out var id))
                {
                    throw new ArgumentException($"Unable to parse the uuid value: {bag.GetString("uuid")}.");
                }

                this.Uuid = id;
            }

            this.Description = bag.GetString("description");

            // According to CTF specification 1.8.2 section 8, if the 'freq' field is not present,
            // it will default to 1 ns.
            this.Frequency = bag.ContainsKey("freq") ? bag.GetUlong("freq") : (ulong) 1000000000;

            if (bag.ContainsKey("offset"))
            {
                this.Offset = bag.GetUlong("offset");
            }
            else if (bag.ContainsKey("offset_s"))
            {
                // todo:this might not be the best approach from an overflow perspective.
                // leaving for now until we have reason to change it
                ulong offsetInSeconds = bag.GetUlong("offset_s");
                this.Offset = offsetInSeconds * this.Frequency;
            }

            if (bag.ContainsKey("precision"))
            {
                this.Precision = bag.GetUlong("precision");
            }

            if (bag.ContainsKey("absolute"))
            {
                this.Absolute = bag.GetBoolean("absolute");
            }
        }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public ulong Frequency { get; }

        /// <inheritdoc />
        public ulong Precision { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public ulong Offset { get; }

        /// <inheritdoc />
        public Guid Uuid { get; }

        /// <inheritdoc />
        public bool Absolute { get; }
    }
}
