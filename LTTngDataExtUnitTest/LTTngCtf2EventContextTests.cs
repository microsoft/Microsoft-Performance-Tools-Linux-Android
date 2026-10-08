// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LTTngDataExtUnitTest
{
    /// <summary>
    /// A CTF 2 event record class can define a specific context, which is recorded between the stream's common
    /// context and the payload.
    /// </summary>
    [TestClass]
    public class LTTngCtf2EventContextTests
    {
        // Context members of different sizes, so a context that isn't read, or is read with the wrong size, shifts
        // every payload field.
        private const string ContextMembers = @"[{""name"":""ctx_flag"",""field-class"":""u8""},{""name"":""ctx_id"",""field-class"":""u32""}]";
        private const string PayloadMembers = @"[{""name"":""value"",""field-class"":""s32""},{""name"":""count"",""field-class"":""u16""}]";

        [TestMethod]
        public void DecodesSpecificContextAndFollowingPayload()
        {
            // Event record class 0 has a specific context; class 1 in the same stream doesn't.
            string metadata = Ctf2TestTraces.Metadata(
                "ust",
                Ctf2TestTraces.EventRecordClass(0, "with_context", PayloadMembers, ContextMembers),
                Ctf2TestTraces.EventRecordClass(1, "without_context", PayloadMembers));

            byte[] stream = Ctf2TestTraces.Packet(
                1000,
                1200,
                Ctf2TestTraces.Event(0, 1000, Context(7, 0x11223344), Payload(-5, 42)),
                Ctf2TestTraces.Event(1, 1100, Payload(-6, 43)),
                Ctf2TestTraces.Event(0, 1200, Context(8, 0x55667788), Payload(-7, 44)));

            var events = Ctf2TestTraces.ProcessGenericEvents(("trace/ust/uid/1000/64-bit", metadata, stream))
                .Select(e => $"{e.EventName}: " + string.Join(", ", e.FieldNames.Zip(e.FieldValues, (name, value) => $"{name} = {value}")))
                .ToList();

            CollectionAssert.AreEqual(
                new[]
                {
                    "with_context: _ctx_flag = 7, _ctx_id = 287454020, _value = -5, _count = 42",
                    "without_context: _value = -6, _count = 43",
                    "with_context: _ctx_flag = 8, _ctx_id = 1432778632, _value = -7, _count = 44",
                },
                events,
                string.Join(Environment.NewLine, events));
        }

        private static byte[] Context(byte flag, uint id)
        {
            return new[] { flag }.Concat(BitConverter.GetBytes(id)).ToArray();
        }

        private static byte[] Payload(int value, ushort count)
        {
            return BitConverter.GetBytes(value).Concat(BitConverter.GetBytes(count)).ToArray();
        }
    }
}
