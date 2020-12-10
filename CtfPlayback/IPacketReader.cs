// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CtfPlayback
{
    /// <summary>
    /// Used to read through a packet in a CTF event stream.
    /// </summary>
    public interface IPacketReader
    {
        /// <summary>
        /// Identifies whether the end of the stream has been reached.
        /// </summary>
        bool EndOfStream { get; }

        /// <summary>
        /// The number of cached bits remaining for consumption.
        /// </summary>
        uint RemainingBufferedBitCount { get; }

        /// <summary>
        /// Read data from the packet.
        /// </summary>
        /// <param name="bitCount">The number of bits to read.</param>
        /// <returns>dThe data read</returns>
        byte[] ReadBits(uint bitCount);

        /// <summary>
        /// Reads a null-terminated string from the packet.
        /// </summary>
        /// <returns>The string as a byte array</returns>
        byte[] ReadString();

        /// <summary>
        /// Align the packet reader to the given bit count.
        /// </summary>
        /// <param name="bitCount"></param>
        void Align(uint bitCount);
    }
}