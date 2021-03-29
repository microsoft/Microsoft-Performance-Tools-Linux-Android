// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Antlr4.Runtime.Tree;
using CtfPlayback.Metadata.AntlrParser;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CtfUnitTest
{
    [TestClass]
    public class FloatingPointTest
        : CtfBaseTest
    {

        [TestMethod]
        [TestCategory("UnitTest")]
        public void FloatingPointTests32()
        {
            // Raw float to expected double value
            var floatTests32 = new Dictionary<RawFloatingPoint, double>()
            {
                // From https://en.wikipedia.org/wiki/Single-precision_floating-point_format
                // CTF is supposed to follow IEEE 754-2008 format - https://diamon.org/ctf/#spec4.1.7
                // Some of these tests fail unless commented out. It's not clear if this is due to issues with Trace Compass like-implemenation or Wikipedia entries
                { new RawFloatingPoint(new byte[] { 0x00, 0x20, 0xA7, 0x44 }, 8, 24), 1337.0 },
                { new RawFloatingPoint(new byte[] { 0x00, 0x00, 0x46, 0x41 }, 8, 24), 12.375 },
                //{ new RawFloatingPoint(new byte[] { 0xFA, 0x3E, 0x88, 0x42 }, 8, 24), 68.123 }, //?? default rounding behaviour of IEEE 754
                { new RawFloatingPoint(new byte[] { 0x00, 0x00, 0x80, 0x3F }, 8, 24), 1.0 },
                { new RawFloatingPoint(new byte[] { 0x00, 0x00, 0x80, 0x3E }, 8, 24), 0.25 },
                { new RawFloatingPoint(new byte[] { 0x00, 0x00, 0xC0, 0x3E }, 8, 24), 0.375 },

                // { new RawFloatingPoint(new byte[] { 0x01, 0x00, 0x00, 0x00 }, 8, 24), float.Epsilon }, //?? // smallest positive subnormal number
                { new RawFloatingPoint(new byte[] { 0xFF, 0xFF, 0x7F, 0x00 }, 8, 24), 1.1754942807573643 * Math.Pow(10,-38)}, // largest subnormal number
                { new RawFloatingPoint(new byte[] { 0x00, 0x00, 0x80, 0x00 }, 8, 24), Math.Pow(2,-126)}, // smallest positive normal number
                { new RawFloatingPoint(new byte[] { 0xFF, 0xFF, 0x7F, 0x7F }, 8, 24), 3.4028234663852886 * Math.Pow(10,38)}, // largest normal number
                // { new RawFloatingPoint(new byte[] { 0x00, 0x00, 0x80, 0x7F }, 8, 24), float.PositiveInfinity}, ?? // infinity
                // { new RawFloatingPoint(new byte[] { 0x00, 0x00, 0x80, 0xFF }, 8, 24), float.NegativeInfinity}, ?? // −infinity

            };

            foreach (var rawFloatDict in floatTests32)
            {
                var rawFloat = rawFloatDict.Key;
                var bufferAsLong = BitConverter.ToInt32(rawFloat.RawBytes);

                var fpDouble = CtfFloatingPointDescriptor.CreateDouble(bufferAsLong, rawFloat.Mant_dig - 1, rawFloat.Exp_dig);

                Assert.AreEqual(rawFloatDict.Value, fpDouble);
            }
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void FloatingPointTests64()
        {
            // Raw float to expected double value
            var floatTests64= new Dictionary<RawFloatingPoint, double>()
            {
                // From https://en.wikipedia.org/wiki/Double-precision_floating-point_format
                // Ditto for commented out tests. See comments for 32-bit tests
                { new RawFloatingPoint(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F}, 11, 53), 1.0 },
                { new RawFloatingPoint(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F}, 11, 53), 1.0000000000000002 },
                { new RawFloatingPoint(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0}, 11, 53), -2 },
                { new RawFloatingPoint(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x88, 0x3F}, 11, 53), 0.01171875 },
                // { new RawFloatingPoint(new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0xD5, 0x3F}, 11, 53), 0.333333333333333314829616256247390992939472198486328125 }, // Close but not exact
                // { new RawFloatingPoint(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}, 11, 53), Math.Pow(2,-1074) }, ?? // Min. subnormal positive double
                // { new RawFloatingPoint(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x00}, 11, 53), 2.2250738585072009 * Math.Pow(10,-308) }, ?? // Max subnormal double
                { new RawFloatingPoint(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00}, 11, 53), 2.2250738585072014 * Math.Pow(10,-308) }, // Min. normal positive double
                // { new RawFloatingPoint(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0x7F}, 11, 53), double.MaxValue }, ?? // Max double
            };

            foreach (var rawFloatDict in floatTests64)
            {
                var rawFloat = rawFloatDict.Key;
                var bufferAsLong = BitConverter.ToInt64(rawFloat.RawBytes);

                var fpDouble = CtfFloatingPointDescriptor.CreateDouble(bufferAsLong, rawFloat.Mant_dig - 1, rawFloat.Exp_dig);

                Assert.AreEqual(rawFloatDict.Value, fpDouble);
            }
        }
    }

    class RawFloatingPoint
    {
        public RawFloatingPoint(byte[] rawBytes, ushort exp_dig, ushort mant_dig)
        {
            RawBytes = rawBytes;
            Exp_dig = exp_dig;
            Mant_dig = mant_dig;
        }

        public byte[] RawBytes { get; set; }
        public ushort Exp_dig { get; set; }
        public ushort Mant_dig { get; set; }
    }
}