// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CtfPlayback
{
    internal interface IPacketReaderContext
    {
        ulong BitsInPacket { get; }

        ulong BitsReadFromPacket { get; }

        void SetPacketSize(ulong bitCount);

        void ReadToEndOfPacket();
    }
}