// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.TypeInterfaces;
using System;
using System.Diagnostics;

namespace CtfPlayback.Metadata.NamedScopes
{
    /// <summary>
    /// Information about the trace itself.
    /// </summary>
    internal class CtfTraceDescriptor
        : ICtfTraceDescriptor
    {
        internal CtfTraceDescriptor(CtfPropertyBag bag, ICtfStructDescriptor packetHeader)
        {
            Debug.Assert(bag != null);

            // According to CTF specification 1.8.2 section C.Examples."Minimal Examples", the packetHeader
            // isn't required if the entire trace consists of a single packet - so no null check here.

            // According to CTF specification 1.8.2 section C.Examples."Minimal Examples", the fields
            // "major", "minor", and "byte_order" are required.

            if (!bag.ContainsKey("major"))
            {
                throw new ArgumentException("Trace declaration does not contain 'major' field.");
            }

            if(!bag.ContainsKey("minor"))
            {
                throw new ArgumentException("Trace declaration does not contain 'major' field.");
            }

            if(!bag.ContainsKey("byte_order"))
            {
                throw new ArgumentException("Trace declaration does not contains 'byte_order' field.");
            }

            this.Major = bag.GetShort("major");
            this.Minor = bag.GetShort("minor");
            this.ByteOrder = bag.GetString("byte_order").Replace("\"", string.Empty);

            Guid id = Guid.Empty;
            if (bag.TryGetString("uuid", out string uuid))
            {
                uuid = uuid.Replace("\"", string.Empty);
                if (!Guid.TryParse(uuid, out id))
                {
                    throw new ArgumentException($"Unable to parse the uuid value: {bag.GetString("uuid")}.");
                }
            }
            this.Uuid = id;

            this.PacketHeader = packetHeader;
        }

        public short Major { get; }

        public short Minor { get; }

        public Guid Uuid { get; }

        public string ByteOrder { get; }

        public ICtfStructDescriptor PacketHeader { get;}
    }
}