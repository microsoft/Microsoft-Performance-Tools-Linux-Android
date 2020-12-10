// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.TypeInterfaces;

namespace CtfUnitTest
{
    public class TestCtfEventDescriptor
        : ICtfEventDescriptor
    {
        public ICtfTypeDescriptor Context { get; set; }

        public ICtfTypeDescriptor Payload { get; set; }

        public IReadOnlyDictionary<string, string> Assignments { get; set; }

        public IReadOnlyDictionary<string, ICtfTypeDescriptor> TypeDeclarations { get; set; }

        public uint? id;
        public uint Id
        {
            get
            {
                if (this.id.HasValue)
                {
                    return this.id.Value;
                }

                if (!this.Assignments.TryGetValue("id", out var idString))
                {
                    throw new Exception("Id was not present in the event descriptor.");
                }

                var idIntegerLiteral = IntegerLiteral.CreateIntegerLiteral(idString);
                if (!idIntegerLiteral.TryGetUInt32(out var idValue))
                {
                    throw new Exception("Id was not a uint.");
                }

                this.id = idValue;

                return idValue;
            }
        }
    }
}