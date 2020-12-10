// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using CtfPlayback.Metadata.InternalHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CtfUnitTest
{
    [TestClass]
    public class IntegerLiteralStringTests
    {
        [TestCategory("UnitTest")]
        [DataTestMethod]
        [DataRow("0", "")]
        [DataRow("37891640", "")]
        [DataRow("0", "u")]
        [DataRow("13", "u")]
        [DataRow("0", "lu")]
        [DataRow("98165443219", "LLU")]
        [DataRow("0", "uLl")]
        [DataRow("18937891", "ulL")]
        [DataRow("98165443219", "LLU")]
        [DataRow("98165443219", "LLU")]
        public void DecimalValues(string plainNumber, string numberSuffix)
        {
            string integerString = plainNumber + numberSuffix;

            var value = new IntegerLiteralString(integerString);

            Assert.IsTrue(StringComparer.InvariantCulture.Equals(value.PlainNumber, plainNumber));
            Assert.AreEqual(numberSuffix, value.NumberSuffix);
            Assert.AreEqual(NumberRadixFormat.Decimal, value.Radix);
        }

        [DataTestMethod]
        [TestCategory("UnitTest")]
        [DataRow("0x0", "")]
        [DataRow("0x3A6490F", "ull")]
        public void HexadecimalValues(string plainNumber, string numberSuffix)
        {
            string integerString = plainNumber + numberSuffix;

            var value = new IntegerLiteralString(integerString);

            Assert.IsTrue(StringComparer.InvariantCulture.Equals(value.PlainNumber, plainNumber));
            Assert.AreEqual(numberSuffix, value.NumberSuffix);
            Assert.AreEqual(NumberRadixFormat.Hexadecimal, value.Radix);
        }

        [DataTestMethod]
        [TestCategory("UnitTest")]
        [DataRow("00", "")]
        [DataRow("0500", "ull")]
        public void OctalValues(string plainNumber, string numberSuffix)
        {
            string integerString = plainNumber + numberSuffix;

            var value = new IntegerLiteralString(integerString);

            Assert.IsTrue(StringComparer.InvariantCulture.Equals(value.PlainNumber, plainNumber));
            Assert.AreEqual(numberSuffix, value.NumberSuffix);
            Assert.AreEqual(NumberRadixFormat.Octal, value.Radix);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void DecimalAsHexadecimalInteger()
        {
            string plainNumber = "562";
            string numberSuffix = string.Empty;

            string integerString = plainNumber + numberSuffix;

            Assert.ThrowsException<ArgumentException>(
                () => new IntegerLiteralString(integerString, NumberRadixFormat.Hexadecimal));
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void HexadecimalAsDecimalInteger()
        {
            string plainNumber = "0x258";
            string numberSuffix = string.Empty;

            string integerString = plainNumber + numberSuffix;

            Assert.ThrowsException<ArgumentException>(
                () => new IntegerLiteralString(integerString, NumberRadixFormat.Decimal));
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void OctalAsDecimalInteger()
        {
            string plainNumber = "0257";
            string numberSuffix = string.Empty;
            string integerString = plainNumber + numberSuffix;

            Assert.ThrowsException<ArgumentException>(
                () => new IntegerLiteralString(integerString, NumberRadixFormat.Decimal));
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void OctalAsHexadecimalInteger()
        {
            string plainNumber = "0315613";
            string numberSuffix = string.Empty;
            string integerString = plainNumber + numberSuffix;

            Assert.ThrowsException<ArgumentException>(
                () => new IntegerLiteralString(integerString, NumberRadixFormat.Hexadecimal));
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void HexadecimalAsOctalInteger()
        {
            string plainNumber = "0x0315613";
            string numberSuffix = string.Empty;
            string integerString = plainNumber + numberSuffix;

            Assert.ThrowsException<ArgumentException>(
                () => new IntegerLiteralString(integerString, NumberRadixFormat.Octal));
        }
    }
}