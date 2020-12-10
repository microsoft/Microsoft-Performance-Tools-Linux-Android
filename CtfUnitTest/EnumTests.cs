// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
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
    public class EnumTests
        : CtfBaseTest
    {
        private void ValidateEnumValue(
            bool signedBaseInteger,
            ICtfNamedRange enumValue,
            EnumeratorTestValue expectedValue)
        {
            if (signedBaseInteger)
            {
                ValidateSignedEnumValue(enumValue, expectedValue);
            }
            else
            {
                ValidateUnsignedEnumValue(enumValue, expectedValue);
            }
        }

        private void ValidateSignedEnumValue(
            ICtfNamedRange enumValue,
            EnumeratorTestValue expectedValue)
        {
            Assert.IsTrue(enumValue.Ranges[0].Begin.Signed);
            Assert.IsTrue(enumValue.Ranges[0].End.Signed);

            Assert.AreEqual(expectedValue.StartValue, enumValue.Ranges[0].Begin.ValueAsLong);
            if (!expectedValue.Range)
            {
                Assert.AreEqual(enumValue.Ranges[0].Begin.ValueAsLong, enumValue.Ranges[0].End.ValueAsLong);
            }
            else
            {
                Assert.AreEqual(expectedValue.EndValue, enumValue.Ranges[0].End.ValueAsLong);
            }
        }

        private void ValidateUnsignedEnumValue(
            ICtfNamedRange enumValue,
            EnumeratorTestValue expectedValue)
        {
            Assert.IsFalse(enumValue.Ranges[0].Begin.Signed);
            Assert.IsFalse(enumValue.Ranges[0].End.Signed);

            Assert.AreEqual(expectedValue.StartValue, (long)enumValue.Ranges[0].Begin.ValueAsUlong);
            if (!expectedValue.Range)
            {
                Assert.AreEqual(enumValue.Ranges[0].Begin.ValueAsUlong, enumValue.Ranges[0].End.ValueAsUlong);
            }
            else
            {
                Assert.AreEqual(expectedValue.EndValue, (long)enumValue.Ranges[0].End.ValueAsUlong);
            }
        }

        private void NamedEnumTest(
            string typeName, 
            bool baseTypeSigned, 
            ushort baseTypeSize, 
            IList<EnumeratorTestValue> mappings)
        {
            var metadataText = new StringBuilder(
                    $"enum {typeName} : integer " +
                    $"{{ size = {baseTypeSize.ToString()}; signed = {baseTypeSigned.ToString()}; }} {{ ");

            foreach (var mapping in mappings)
            {
                metadataText.Append(mapping.ToString());
            }

            metadataText.Append("};");

            var parser = GetParser(metadataText.ToString());

            var metadataContext = parser.file();

            this.ValidateEmptyErrorListener();

            var metadataCustomization = new TestCtfMetadataCustomization();

            var listener = new CtfListener(parser, metadataCustomization, metadataCustomization);
            Assert.IsNotNull(listener.GlobalScope);

            var treeWalker = new ParseTreeWalker();
            treeWalker.Walk(listener, metadataContext);

            bool typeExists = listener.GlobalScope.Types.TryGetValue(typeName, out var typeDeclaration);
            Assert.IsTrue(typeExists);

            var ctfMetadataType = typeDeclaration.Type;
            Assert.IsTrue(ctfMetadataType is CtfEnumDescriptor);

            var enumType = (CtfEnumDescriptor)ctfMetadataType;

            Assert.AreEqual(baseTypeSigned, enumType.BaseType.Signed);
            Assert.AreEqual(baseTypeSize, enumType.BaseType.Size);
            // According to spec 1.8.2 section 4.1.5, integers that are a multiple of 8-bits are 8-bit aligned.
            // Otherwise they are one bit aligned
            Assert.AreEqual((baseTypeSize & 0x07) == 0 ? 8 : 1, enumType.Align);

            var enumValues = enumType.EnumeratorValues.ToList();
            var enumValuesByName = new Dictionary<string, ICtfNamedRange>();
            foreach (var enumValue in enumValues)
            {
                enumValuesByName.Add(enumValue.Name, enumValue);
            }

            Assert.AreEqual(mappings.Count, enumValues.Count());
            foreach (var mapping in mappings)
            {
                bool mappingExists = enumValuesByName.TryGetValue(mapping.Name.Trim('"'), out var enumValue);
                Assert.IsTrue(mappingExists);

                ValidateEnumValue(enumType.BaseType.Signed, enumValue, mapping);
            }
        }

        /// <summary>
        /// Test a single value
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest1()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = false;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "only_identifier",
                    StartValue = 5
                },
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }

        /// <summary>
        /// Test two values
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest2()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = false;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "identifier_one",
                    StartValue = 5,
                    AddComma = true
                },
                new EnumeratorTestValue
                {
                    Name = "identifier_two",
                    StartValue = 6
                },
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }

        /// <summary>
        /// Test a range value
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest3()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = false;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "identifier_one",
                    StartValue = 5,
                    EndValue = 15,
                    Range = true
                }
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }

        /// <summary>
        /// Test two values with unspecified values
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest5()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = false;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "identifier_one",
                    StartValue = 0,
                    ValueSpecified = false,
                    AddComma = true
                },
                new EnumeratorTestValue
                {
                    Name = "identifier_two",
                    StartValue = 1,
                    ValueSpecified = false
                },
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }

        /// <summary>
        /// Test two values, the first isn't specified, the second is
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest6()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = false;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "identifier_one",
                    StartValue = 0,
                    ValueSpecified = false,
                    AddComma = true
                },
                new EnumeratorTestValue
                {
                    Name = "identifier_two",
                    StartValue = 10
                },
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }

        /// <summary>
        /// Test two values, the first is specified, the second is isn't
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest7()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = false;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "identifier_one",
                    StartValue = 3,
                    AddComma = true
                },
                new EnumeratorTestValue
                {
                    Name = "identifier_two",
                    StartValue = 4,
                    ValueSpecified = false,
                },
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }


        /// <summary>
        /// The set of enumerator values ends in a comma
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest8()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = false;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "identifier_one",
                    StartValue = 3,
                    AddComma = true
                },
                new EnumeratorTestValue
                {
                    Name = "identifier_two",
                    StartValue = 4,
                    AddComma = true
                },
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }

        /// <summary>
        /// The enumerator value is a keyword
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest9()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = false;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "enum",
                    StartValue = 2
                },
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }

        /// <summary>
        /// All enumerator values are keywords
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest10()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = false;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "enum",
                    AddComma = true,
                    StartValue = 2
                },
                new EnumeratorTestValue
                {
                    Name = "trace",
                    StartValue = 3,
                    ValueSpecified = false
                },
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }

        /// <summary>
        /// The enumerator value is a string literal
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest11()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = false;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "\"this is an enum value\"",
                    StartValue = 2
                },
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }

        /// <summary>
        /// All enumerator values are string literals.
        /// The second and third values aren't specified.
        /// The final value has a range.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest12()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = true;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "\"this is the first enum value\"",
                    StartValue = 2,
                    StartValueIsSigned = false,
                    AddComma = true
                },
                new EnumeratorTestValue
                {
                    Name = "\"this is the second enum value\"",
                    StartValue = 3,
                    AddComma = true,
                    ValueSpecified = false
                },
                new EnumeratorTestValue
                {
                    Name = "\"this is the third enum value\"",
                    StartValue = 4,
                    AddComma = true,
                    ValueSpecified = false,
                },
                new EnumeratorTestValue
                {
                    Name = "\"this is the final enum value\"",
                    StartValue = 12,
                    EndValue = 15,
                    Range = true,
                    EndValueIsSigned = false,
                    AddComma = true
                },
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }

        /// <summary>
        /// This is a mix of enumerator value types: identifiers, keyword, and string literals
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void NamedEnumTest13()
        {
            const string typeName = "EnumType1";
            const bool baseTypeSigned = false;
            const ushort baseTypeSize = 5;

            var integerProperties = new CtfPropertyBag();
            integerProperties.AddValue("size", baseTypeSize.ToString());
            integerProperties.AddValue("signed", baseTypeSigned.ToString());

            var mappings = new List<EnumeratorTestValue>
            {
                new EnumeratorTestValue
                {
                    Name = "\"this is the first enum value\"",
                    StartValue = 2,
                    AddComma = true
                },
                new EnumeratorTestValue
                {
                    Name = "integer",
                    StartValue = 6,
                    AddComma = true
                },
                new EnumeratorTestValue
                {
                    Name = "normal_enum_specifier",
                    StartValue = 7,
                    AddComma = true,
                    ValueSpecified = false,
                },
                new EnumeratorTestValue
                {
                    Name = "\"this is the final enum value\"",
                    StartValue = 12,
                    EndValue = 16,
                    Range = true,
                    AddComma = true
                },
            };

            NamedEnumTest(typeName, baseTypeSigned, baseTypeSize, mappings);
        }
    }
}