// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using CtfPlayback.FieldValues;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CtfUnitTest
{
    [TestClass]
    public class IntegerDescriptorTests
    {
        [TestMethod]
        [DataRow("le")]
        [DataRow("be")]
        public void ReadsSignedIntegersOfEverySize(string byteOrder)
        {
            foreach (int size in new[] { 8, 16, 24, 32, 40, 48, 56, 64 })
            {
                long max = size == 64 ? long.MaxValue : (1L << (size - 1)) - 1;
                long min = size == 64 ? long.MinValue : -(1L << (size - 1));
                var values = new List<long> { 0, 1, -1, max, min, max / 3, min / 3 };
                if (size > 32)
                {
                    // Values with bits at and above bit 31 set, which a 32-bit sign mask mishandles.
                    values.AddRange(new[] { 1L << 31, 1L << 32, -(1L << 32), 0x7FFFFFFFL, 0x80000000L, -0x80000001L });
                }

                foreach (long value in values)
                {
                    Assert.AreEqual(value, ReadSigned(size, byteOrder, value), $"size={size} byteOrder={byteOrder} value=0x{value:X}");
                }
            }
        }

        [TestMethod]
        public void ReadsSigned64BitBigEndian()
        {
            var descriptor = CreateDescriptor(64, signed: true, "be");

            var value = (CtfIntegerValue)descriptor.Read(new byte[] { 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 }, 8);
            Assert.AreEqual(0x0000000100000000L, value.Value.ValueAsLong);

            value = (CtfIntegerValue)descriptor.Read(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE }, 8);
            Assert.AreEqual(-2L, value.Value.ValueAsLong);

            value = (CtfIntegerValue)descriptor.Read(new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 8);
            Assert.IsTrue(value.Value.TryGetInt64(out long minimum));
            Assert.AreEqual(long.MinValue, minimum);
        }

        [TestMethod]
        [DataRow("le")]
        [DataRow("be")]
        public void ReadsUnsigned64Bit(string byteOrder)
        {
            foreach (ulong value in new[] { 0ul, 1ul, 0x80000000ul, 0x100000000ul, ulong.MaxValue })
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (byteOrder == "be")
                {
                    Array.Reverse(bytes);
                }

                var result = (CtfIntegerValue)CreateDescriptor(64, signed: false, byteOrder).Read(bytes, bytes.Length);
                Assert.AreEqual(value, result.Value.ValueAsUlong);
            }
        }

        private static long ReadSigned(int size, string byteOrder, long value)
        {
            // Two's complement, truncated to the integer's size.
            byte[] bytes = BitConverter.GetBytes(value).Take(size / 8).ToArray();
            if (byteOrder == "be")
            {
                Array.Reverse(bytes);
            }

            var result = (CtfIntegerValue)CreateDescriptor(size, signed: true, byteOrder).Read(bytes, bytes.Length);
            return result.Value.ValueAsLong;
        }

        private static CtfIntegerDescriptor CreateDescriptor(int size, bool signed, string byteOrder)
        {
            var bag = new CtfPropertyBag();
            bag.AddValue("size", size.ToString());
            bag.AddValue("align", "8");
            bag.AddValue("signed", signed ? "true" : "false");
            bag.AddValue("byte_order", byteOrder);
            return new CtfIntegerDescriptor(bag);
        }
    }
}
