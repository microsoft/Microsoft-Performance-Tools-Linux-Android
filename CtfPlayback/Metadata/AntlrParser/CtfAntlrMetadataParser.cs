// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CtfPlayback.Metadata.Interfaces;

namespace CtfPlayback.Metadata.AntlrParser
{
    /// <summary>
    /// Implement ICtfMetadataParser using an Antlr grammar.
    /// </summary>
    public sealed class CtfAntlrMetadataParser 
        : ICtfMetadataParser
    {
        private CtfParser parser;
        private CtfListener listener;
        private readonly ICtfMetadataCustomization metadataCustomization;
        private readonly ICtfMetadataBuilder metadataBuilder;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="metadataCustomization">Extension points for parsing metadata</param>
        /// <param name="metadataBuilder">The object used to build metadata</param>
        public CtfAntlrMetadataParser(
            ICtfMetadataCustomization metadataCustomization, 
            ICtfMetadataBuilder metadataBuilder)
        {
            this.metadataCustomization = metadataCustomization;
            this.metadataBuilder = metadataBuilder;
        }

        /// <summary>
        /// Parse the CTF stream.
        /// </summary>
        /// <param name="metadataStream">Metadata stream</param>
        /// <returns>Metadata parsed from the stream</returns>
        public ICtfMetadata Parse(Stream metadataStream)
        {
            string metadata = this.GetMetadata(metadataStream);

            this.parser = GetContext(metadata);

            // Error listeners can be switched out for debugging purposes. Leaving these comments here for easy access.

            //this.parser.RemoveErrorListeners();
            //this.parser.AddErrorListener(new DebugErrorListener());
            //this.parser.AddErrorListener(new DiagnosticErrorListener(false));

            var fileContext = this.parser.file();
            this.listener = new CtfListener(this.parser, this.metadataCustomization, this.metadataBuilder);

            var treeWalker = new ParseTreeWalker();
            treeWalker.Walk(this.listener, fileContext);

            return this.listener.GlobalScope;
        }

        /// <summary>
        /// Converts the metadata stream into a string.
        /// </summary>
        /// <param name="metadataStream">Metadata stream</param>
        /// <returns>String representation of the metadata</returns>
        private unsafe string GetMetadata(Stream metadataStream)
        {
            byte[] headerBuffer = new byte[Marshal.SizeOf(typeof(MetadataPacketHeader))];
            byte[] buffer = null;

            long originalPosition = 0;
            if (metadataStream.CanSeek)
            {
                originalPosition = metadataStream.Position;
            }

            var sb = new StringBuilder();

            while (true)
            {
                int bytesRead = metadataStream.Read(headerBuffer, 0, headerBuffer.Length);
                if (bytesRead == 0)
                {
                    break;
                }

                MetadataPacketHeader header;
                fixed (byte* ptr = headerBuffer)
                {
                    header = *((MetadataPacketHeader*)ptr);
                }

                // todo:check the endianness based on the magic number

                if (header.Magic == 0x75d11d57) // CTF: Magic Header for binary metadata streams // 1976638807
                {
                    int packetSize = (int) header.PacketSize / 8 - headerBuffer.Length;

                    if (buffer == null || buffer.Length < packetSize)
                    {
                        buffer = new byte[packetSize];
                    }

                    int read = metadataStream.Read(buffer, 0, packetSize);
                    if (read == 0)
                    {
                        break;
                    }

                    if (read != packetSize) // .NET 6.0 breaking change - https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/partial-byte-reads-in-streams - bytes read can be less than what was requested
                    {
                        while (read < buffer.Length) // Keep reading until we fill up our buffer to the packetSize
                        {
                            int tmpBytesRead = metadataStream.Read(buffer.AsSpan().Slice(read));
                            if (tmpBytesRead == 0) break;
                            read += tmpBytesRead;
                        }
                    }

                    int contentSize = (int) header.ContentSize / 8 - headerBuffer.Length;
                    if (contentSize < read)
                    {
                        read = contentSize;
                    }

                    string result = Encoding.ASCII.GetString(buffer, 0, read);

                    sb.Append(result);
                }
                else
                {
                    if (!metadataStream.CanSeek)
                    {
                        var memStream = new MemoryStream();
                        metadataStream.CopyTo(memStream);   // Decompress
                        memStream.Position = 0;             // Copy sets position at end

                        using (var textReader = new StreamReader(memStream))
                        {
                            return Encoding.UTF8.GetString(headerBuffer) + textReader.ReadToEnd(); // Need to concat original "header" (which wasn't really a header) with rest of file
                        }
                    }

                    metadataStream.Position = originalPosition;
                    using (var textReader = new StreamReader(metadataStream, Encoding.Default, false, 1024 * 1024, true))
                    {
                        return textReader.ReadToEnd();
                    }
                }
            }

            return sb.ToString();
        }

        private static CtfParser GetContext(string value)
        {
            using (var inputStream = StreamFromString(value))
            {
                return GetContext(inputStream);
            }
        }

        private static CtfParser GetContext(Stream inputStream)
        {
            var input = new AntlrInputStream(inputStream);
            var lexer = new CtfLexer(input);
            //lexer.RemoveErrorListeners();
            var tokens = new CommonTokenStream(lexer);
            return new CtfParser(tokens);
        }

        private static Stream StreamFromString(string value)
        {
            var memoryStream = new MemoryStream();

            var writer = new StreamWriter(memoryStream);
            writer.Write(value);
            writer.Flush();

            memoryStream.Position = 0;

            return memoryStream;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MetadataPacketHeader
        {
            public readonly uint Magic;
            public readonly Guid Uuid;
            public readonly uint Checksum;
            public readonly uint ContentSize;
            public readonly uint PacketSize;
            public readonly byte CompressionScheme;
            public readonly byte EncryptionScheme;
            public readonly byte ChecksumScheme;
            public readonly byte Major;
            public readonly byte Minor;
        }
    }
}