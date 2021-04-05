// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
            // Raw float to expected float value
            var floatTests32 = new Dictionary<byte[], float>()
            {
                // Some tests - Little Endian - bytes in reverse order
                { new byte[] { 0xCD, 0xCC, 0xF6, 0x42 }, 123.4f },
                { new byte[] { 0x00, 0x60, 0xB7, 0xC2 }, -91.6875f },
                { new byte[] { 0x00, 0x00, 0xC8, 0x3C }, 0.0244140625f},
                
                // From https://en.wikipedia.org/wiki/Single-precision_floating-point_format
                // CTF is supposed to follow IEEE 754-2008 format - https://diamon.org/ctf/#spec4.1.7
                { new byte[] { 0x00, 0x00, 0x20, 0x3E }, 0.15625f },
                { new byte[] { 0x00, 0x20, 0xA7, 0x44 }, 1337.0f },
                { new byte[] { 0x00, 0x00, 0x46, 0x41 }, 12.375f },
                { new byte[] { 0xFA, 0x3E, 0x88, 0x42 }, 68.123f },
                { new byte[] { 0x00, 0x00, 0x80, 0x3F }, 1.0f },
                { new byte[] { 0x00, 0x00, 0x80, 0x3E }, 0.25f },
                { new byte[] { 0x00, 0x00, 0xC0, 0x3E }, 0.375f },

                { new byte[] { 0x01, 0x00, 0x00, 0x00 }, float.Epsilon }, // smallest positive subnormal number
                { new byte[] { 0xFF, 0xFF, 0x7F, 0x00 }, (float) (1.1754942107 * Math.Pow(10,-38))}, // largest subnormal number
                { new byte[] { 0x00, 0x00, 0x80, 0x00 }, (float) Math.Pow(2,-126)}, // smallest positive normal number
                { new byte[] { 0xFF, 0xFF, 0x7F, 0x7F }, (float) (3.4028234663852886 * Math.Pow(10,38))}, // largest normal number
                { new byte[] { 0x00, 0x00, 0x80, 0x7F }, float.PositiveInfinity}, // infinity
                { new byte[] { 0x00, 0x00, 0x80, 0xFF }, float.NegativeInfinity}, // −infinity
                { new byte[] { 0x01, 0x00, 0xC0, 0xFF }, float.NaN}, // qNaN (on x86 and ARM processors)
            };

            foreach (var rawFloatDict in floatTests32)
            {
                var rawFloat = rawFloatDict.Key;
                var bufferAsInt = BitConverter.ToInt32(rawFloat);
                var flt = CtfFloatingPointDescriptor.CreateFloat(bufferAsInt);

                Assert.AreEqual(rawFloatDict.Value, flt);
            }
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void FloatingPointTests64()
        {
            // Raw float to expected double value
            var floatTests64= new Dictionary<byte[], double>()
            {
                // Some tests - Little Endian
                { new byte[] { 0x00, 0x00, 0x00, 0x60, 0x66, 0xBE, 0x81, 0x40}, 567.8f  }, // Note 567.8f = 567.79998779296875 double

                // From https://en.wikipedia.org/wiki/Double-precision_floating-point_format
                // Ditto for commented out tests. See comments for 32-bit tests
                { new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F}, 1.0 },
                { new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F}, 1.0000000000000002 },
                { new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0}, -2 },
                { new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x88, 0x3F}, 0.01171875 },
                { new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0xD5, 0x3F}, 0.3333333333333333 }, // ~1/3 as that can not exactly be specified as double
                { new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}, Math.Pow(2,-1074) },  // Min. subnormal positive double
                { new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x00}, 2.225073858507201 * Math.Pow(10,-308) }, // Max subnormal double
                { new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00}, 2.2250738585072014 * Math.Pow(10,-308) }, // Min. normal positive double
                { new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0x7F}, double.MaxValue }, //?? // Max double
                { new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x7F}, double.PositiveInfinity }, // +∞ (positive infinity)
                { new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0xFF}, double.NegativeInfinity }, // +∞ (negative infinity)
                { new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x7F}, double.NaN }, //?? // NaN (sNaN on most processors, such as x86 and ARM)
                { new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF8, 0x7F}, double.NaN }, //?? // NaN (qNaN on most processors, such as x86 and ARM)
            };

            foreach (var rawDoubleDict in floatTests64)
            {
                var rawDouble = rawDoubleDict.Key;
                var bufferAsLong = BitConverter.ToInt64(rawDouble);
                var fpDouble = CtfFloatingPointDescriptor.CreateDouble(bufferAsLong);

                Assert.AreEqual(rawDoubleDict.Value, fpDouble);
            }
        }
    }
}