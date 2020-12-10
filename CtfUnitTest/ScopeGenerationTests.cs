// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Antlr4.Runtime.Tree;
using CtfPlayback.Metadata;
using CtfPlayback.Metadata.AntlrParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CtfUnitTest
{
    /// <summary>
    /// These tests confirm that scope generation happens for a CTF metadata file as expected.
    /// The following are expected to generate scopes:
    /// 1. Global scope
    /// 2. Trace, Env, Stream, Event, Clock
    /// 3. Struct, Variant, Enum &lt;= temporary scopes that are converted into types
    /// </summary>
    [TestClass]
    public class ScopeGenerationTests
        : CtfBaseTest
    {
        /// <summary>
        /// The global scope should be created immediately, and be available the entirety of the listener.
        /// Note that an empty string is not a valid metadata file. We expect at least a single declaration.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void GlobalScopeExists()
        {
            var metadataText = "";
            var parser = GetParser(metadataText);

            var metadataContext = parser.file();
            var metadataCustomization = new TestCtfMetadataCustomization();

            var listener = new CtfListener(parser, metadataCustomization, metadataCustomization);
            Assert.IsNotNull(listener.GlobalScope);
        }

        /// <summary>
        /// The simplest trace declaration available should generate a trace scope.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void TraceScopeGenerated()
        {
            var metadataText = 
                "trace {" +
                " major = 1;" +
                " minor = 8;" +
                " byte_order = \"le\";" +
                " packet.header := struct { };" +
                "};";
            var parser = GetParser(metadataText);

            var metadataContext = parser.file();

            this.ValidateEmptyErrorListener();

            var metadataCustomization = new TestCtfMetadataCustomization();
            var listener = new CtfListener(parser, metadataCustomization, metadataCustomization);
            Assert.IsNotNull(listener.GlobalScope);

            var treeWalker = new ParseTreeWalker();
            treeWalker.Walk(listener, metadataContext);

            Assert.IsTrue(listener.GlobalScope.Children.Count == 1);
            Assert.IsTrue(StringComparer.CurrentCulture.Equals(listener.GlobalScope.Children.First().Key,"trace"));
            Assert.IsNotNull(listener.GlobalScope.Children["trace"]);
        }

        /// <summary>
        /// The simplest trace declaration available should generate a trace scope.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void TraceWithoutPacketHeaderThrows()
        {
            var metadataText = 
                "trace {" +
                " major = 1;" +
                " minor = 8;" +
                "};";
            var parser = GetParser(metadataText);

            var metadataContext = parser.file();

            this.ValidateEmptyErrorListener();

            var metadataCustomization = new TestCtfMetadataCustomization();
            var listener = new CtfListener(parser, metadataCustomization, metadataCustomization);
            Assert.IsNotNull(listener.GlobalScope);

            var treeWalker = new ParseTreeWalker();
            Assert.ThrowsException<CtfMetadataException>(() => treeWalker.Walk(listener, metadataContext));
        }

        /// <summary>
        /// The simplest env declaration available should generate an env scope.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void EnvScopeGenerated()
        {
            var metadataText = "env {};";
            var parser = GetParser(metadataText);

            var metadataContext = parser.file();

            this.ValidateEmptyErrorListener();

            var metadataCustomization = new TestCtfMetadataCustomization();
            var listener = new CtfListener(parser, metadataCustomization, metadataCustomization);
            Assert.IsNotNull(listener.GlobalScope);

            var treeWalker = new ParseTreeWalker();
            treeWalker.Walk(listener, metadataContext);

            Assert.IsTrue(listener.GlobalScope.Children.Count == 1);
            Assert.IsTrue(StringComparer.CurrentCulture.Equals(listener.GlobalScope.Children.First().Key, "env"));
            Assert.IsNotNull(listener.GlobalScope.Children["env"]);
        }

        /// <summary>
        /// The simplest clock declaration available should generate an clock scope.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void ClockScopeGenerated()
        {
            var metadataText = "clock { name = \"testClock\"; };";
            var parser = GetParser(metadataText);

            var metadataContext = parser.file();

            this.ValidateEmptyErrorListener();

            var metadataCustomization = new TestCtfMetadataCustomization();
            var listener = new CtfListener(parser, metadataCustomization, metadataCustomization);
            Assert.IsNotNull(listener.GlobalScope);

            var treeWalker = new ParseTreeWalker();
            treeWalker.Walk(listener, metadataContext);

            Assert.IsTrue(listener.GlobalScope.Children.Count == 1);
            Assert.IsTrue(StringComparer.CurrentCulture.Equals(listener.GlobalScope.Children.First().Key, "clock"));
            Assert.IsNotNull(listener.GlobalScope.Children["clock"]);
        }

        /// <summary>
        /// A clock scope must have a name according to CTF specification 1.8.2 section 8.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void UnnamedClockThrows()
        {
            var metadataText = "clock {};";
            var parser = GetParser(metadataText);

            var metadataContext = parser.file();

            this.ValidateEmptyErrorListener();

            var metadataCustomization = new TestCtfMetadataCustomization();
            var listener = new CtfListener(parser, metadataCustomization, metadataCustomization);
            Assert.IsNotNull(listener.GlobalScope);

            var treeWalker = new ParseTreeWalker();
            Assert.ThrowsException<CtfMetadataException>(
                () => treeWalker.Walk(listener, metadataContext));
        }

        /// <summary>
        /// The simplest stream declaration available should generate an stream scope.
        /// The stream is anonymous until it has an id, so we just expect the scope name to contain [stream].
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void StreamScopeGenerated()
        {
            var metadataText = 
                "stream { " +
                "    event.header := struct { };" +
                "    packet.context := struct { };" +
                "};";
            var parser = GetParser(metadataText);

            var metadataContext = parser.file();

            this.ValidateEmptyErrorListener();

            var metadataCustomization = new TestCtfMetadataCustomization();
            var listener = new CtfListener(parser, metadataCustomization, metadataCustomization);
            Assert.IsNotNull(listener.GlobalScope);

            var treeWalker = new ParseTreeWalker();
            treeWalker.Walk(listener, metadataContext);

            Assert.IsTrue(listener.GlobalScope.Children.Count == 1);
            Assert.IsTrue(listener.GlobalScope.Children.First().Value.Name.Contains("[stream]"));
        }

        /// <summary>
        /// This metadata creates a temporary integer type in the global scope, but should go away
        /// and a type should be added. Type addition will be tested in a different test.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void IntegerScopeGenerated()
        {
            var metadataText =
                "typealias integer { size=32; } := uint32;";

            var parser = GetParser(metadataText);

            var metadataContext = parser.file();

            this.ValidateEmptyErrorListener();

            var metadataCustomization = new TestCtfMetadataCustomization();
            var listener = new CtfListener(parser, metadataCustomization, metadataCustomization);
            Assert.IsNotNull(listener.GlobalScope);

            var treeWalker = new ParseTreeWalker();
            treeWalker.Walk(listener, metadataContext);

            Assert.IsTrue(listener.GlobalScope.Children.Count == 0);
        }

        /// <summary>
        /// This metadata creates a string type in the global scope.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void EmptyStringGeneratesNoScope()
        {
            var metadataText =
                "typealias string { } := utf8_string;";

            var parser = GetParser(metadataText);

            var metadataContext = parser.file();

            this.ValidateEmptyErrorListener();

            var metadataCustomization = new TestCtfMetadataCustomization();
            var listener = new CtfListener(parser, metadataCustomization, metadataCustomization);
            Assert.IsNotNull(listener.GlobalScope);

            var treeWalker = new ParseTreeWalker();
            treeWalker.Walk(listener, metadataContext);

            Assert.IsTrue(listener.GlobalScope.Children.Count == 0);
        }

        /// <summary>
        /// This metadata creates a temporary string type in the global scope, but should go away
        /// and a type should be added. Type addition will be tested in a different test.
        /// </summary>
        [TestMethod]
        [TestCategory("UnitTest")]
        public void StringScopeGenerated()
        {
            var metadataText =
                "typealias string { encoding=UTF8; } := utf8_string;";

            var parser = GetParser(metadataText);

            var metadataContext = parser.file();

            this.ValidateEmptyErrorListener();

            var metadataCustomization = new TestCtfMetadataCustomization();
            var listener = new CtfListener(parser, metadataCustomization, metadataCustomization);
            Assert.IsNotNull(listener.GlobalScope);

            var treeWalker = new ParseTreeWalker();
            treeWalker.Walk(listener, metadataContext);

            Assert.IsTrue(listener.GlobalScope.Children.Count == 0);
        }
    }
}
