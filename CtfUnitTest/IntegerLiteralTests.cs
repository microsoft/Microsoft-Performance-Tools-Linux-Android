// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.InternalHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CtfUnitTest
{
    [TestClass]
    public class IntegerLiteralTests
    {
        [TestMethod]
        [TestCategory("UnitTest")]
        public void Signed1BitInteger()
        {
            string plainNumber = "1";
            string numberSuffix = string.Empty;
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            var value = new IntegerLiteral(stringRepresentation);

            Assert.AreEqual(!numberSuffix.Contains('u'), value.Signed);
            Assert.AreEqual(2, value.RequiredBitCount);
            Assert.AreEqual(1, value.ValueAsLong);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void Signed4bitInteger()
        {
            string plainNumber = "4";
            string numberSuffix = string.Empty;
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            var value = new IntegerLiteral(stringRepresentation);

            Assert.AreEqual(!numberSuffix.Contains('u'), value.Signed);
            Assert.AreEqual(4, value.RequiredBitCount);
            Assert.AreEqual(4, value.ValueAsLong);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void Unsigned3BitInteger()
        {
            string plainNumber = "4";
            string numberSuffix = "u";
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            var value = new IntegerLiteral(stringRepresentation);

            Assert.AreEqual(!numberSuffix.Contains('u'), value.Signed);
            Assert.AreEqual(3, value.RequiredBitCount);
            Assert.AreEqual(4ul, value.ValueAsUlong);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void Signed8BitInteger()
        {
            sbyte startingValue = sbyte.MaxValue;
            string plainNumber = startingValue.ToString();
            string numberSuffix = string.Empty;
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            var value = new IntegerLiteral(stringRepresentation);

            Assert.AreEqual(!numberSuffix.Contains('u'), value.Signed);
            Assert.AreEqual(8, value.RequiredBitCount);
            Assert.AreEqual(startingValue, value.ValueAsLong);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void SignedHigh9BitsInteger()
        {
            short startingValue = sbyte.MaxValue + 1;
            string plainNumber = startingValue.ToString();
            string numberSuffix = string.Empty;
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            var value = new IntegerLiteral(stringRepresentation);

            Assert.AreEqual(!numberSuffix.Contains('u'), value.Signed);
            Assert.AreEqual(9, value.RequiredBitCount);
            Assert.AreEqual(startingValue, value.ValueAsLong);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void Unsigned8BitInteger()
        {
            byte startingValue = byte.MaxValue;
            string plainNumber = startingValue.ToString();
            string numberSuffix = "u";
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            var value = new IntegerLiteral(stringRepresentation);

            Assert.AreEqual(!numberSuffix.Contains('u'), value.Signed);
            Assert.AreEqual(8, value.RequiredBitCount);
            Assert.AreEqual(startingValue, value.ValueAsUlong);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void UnsignedHigh9BitsInteger()
        {
            ushort startingValue = byte.MaxValue + 1;
            string plainNumber = startingValue.ToString();
            string numberSuffix = "u";
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            var value = new IntegerLiteral(stringRepresentation);

            Assert.AreEqual(!numberSuffix.Contains('u'), value.Signed);
            Assert.AreEqual(9, value.RequiredBitCount);
            Assert.AreEqual(startingValue, value.ValueAsUlong);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void Signed32BitsInteger()
        {
            int startingValue = int.MaxValue;
            string plainNumber = startingValue.ToString();
            string numberSuffix = string.Empty;
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            var value = new IntegerLiteral(stringRepresentation);

            Assert.AreEqual(!numberSuffix.Contains('u'), value.Signed);
            Assert.AreEqual(32, value.RequiredBitCount);
            Assert.AreEqual(startingValue, value.ValueAsLong);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void Unsigned32BitsInteger()
        {
            uint startingValue = uint.MaxValue;
            string plainNumber = startingValue.ToString();
            string numberSuffix = "u";
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            var value = new IntegerLiteral(stringRepresentation);

            Assert.AreEqual(!numberSuffix.Contains('u'), value.Signed);
            Assert.AreEqual(32, value.RequiredBitCount);
            Assert.AreEqual(startingValue, value.ValueAsUlong);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void Signed64BitsInteger()
        {
            long startingValue = long.MaxValue;
            string plainNumber = startingValue.ToString();
            string numberSuffix = string.Empty;
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            var value = new IntegerLiteral(stringRepresentation);

            Assert.AreEqual(!numberSuffix.Contains('u'), value.Signed);
            Assert.AreEqual(64, value.RequiredBitCount);
            Assert.AreEqual(startingValue, value.ValueAsLong);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void Unsigned64BitsInteger()
        {
            ulong startingValue = ulong.MaxValue;
            string plainNumber = startingValue.ToString();
            string numberSuffix = "u";
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            var value = new IntegerLiteral(stringRepresentation);

            Assert.AreEqual(!numberSuffix.Contains('u'), value.Signed);
            Assert.AreEqual(64, value.RequiredBitCount);
            Assert.AreEqual(startingValue, value.ValueAsUlong);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void SignedTooBigInteger()
        {
            string plainNumber = "9223372036854775808";
            string numberSuffix = string.Empty;
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            Assert.ThrowsException<NotSupportedException>(
                () => new IntegerLiteral(stringRepresentation));
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void UnsignedTooBigInteger()
        {
            string plainNumber = "18446744073709551617";
            string numberSuffix = "u";
            string integerString = plainNumber + numberSuffix;
            var stringRepresentation = new IntegerLiteralString(integerString);

            Assert.ThrowsException<NotSupportedException>(
                () => new IntegerLiteral(stringRepresentation));
        }
    }
}