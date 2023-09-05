// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.IO;

//TODO: Optimize reading stream with Span/Memory in .net6+

internal sealed class ChunkReader : IChunkReader
{
    private const int ChunkHeaderSize = 2;
    private int _readTimeoutMs = -1;
    private readonly Memory<byte> _buffer;

    internal ChunkReader(Stream downStream)
    {
        InputStream = downStream ?? throw new ArgumentNullException(nameof(downStream));
        Throw.ArgumentOutOfRangeException.IfFalse(downStream.CanRead, nameof(downStream.CanRead));
        _buffer = new Memory<byte>(new byte[Constants.ChunkBufferSize * 4]);
    }

    private Stream InputStream { get; }
    private MemoryStream ChunkBuffer { get; set; }
    private long ChunkBufferRemaining => ChunkBuffer.Length - ChunkBuffer.Position;

    public async ValueTask<int> ReadMessageChunksToBufferStreamAsync(Stream bufferStream)
    {
        var messageCount = 0;
        //store output streams state, and ensure we add to the end of it.
        var previousStreamPosition = bufferStream.Position;
        bufferStream.Position = bufferStream.Length;

        using (ChunkBuffer = new MemoryStream())
        {
            //Use this as we need an initial state < ChunkBuffer.Length
            long chunkBufferPosition = -1;

            //We have not finished parsing the chunk buffer, so further messages to de-chunk
            while (chunkBufferPosition < ChunkBuffer.Length)
            {
                if (await ConstructMessageAsync(bufferStream).ConfigureAwait(false))
                {
                    messageCount++;
                }

                chunkBufferPosition = ChunkBuffer.Position;
            }
        }

        //restore output streams state.
        bufferStream.Position = previousStreamPosition;
        return messageCount;
    }

    public void SetTimeoutInMs(int ms)
    {
        _readTimeoutMs = ms;
    }

    private void ChunkBufferTrimUsedData()
    {
        //Remove 'used' data from memory stream, that is everything before it's current position
        var internalBuffer = ChunkBuffer.GetBuffer();
        Buffer.BlockCopy(internalBuffer, (int)ChunkBuffer.Position, internalBuffer, 0, (int)ChunkBufferRemaining);
        ChunkBuffer.SetLength((int)ChunkBufferRemaining);
        ChunkBuffer.Position = 0;
    }

    private async ValueTask PopulateChunkBufferAsync(int requiredSize = Constants.ChunkBufferSize)
    {
        if (ChunkBufferRemaining >= requiredSize)
        {
            return;
        }

        ChunkBufferTrimUsedData();

        var storedPosition = ChunkBuffer.Position;
        requiredSize -= (int)ChunkBufferRemaining;
        ChunkBuffer.Position = ChunkBuffer.Length;

        while (requiredSize > 0)
        {
            var numBytesRead = await InputStream
                .ReadWithTimeoutAsync(_buffer, _readTimeoutMs)
                .ConfigureAwait(false);

            if (numBytesRead <= 0)
            {
                break;
            }

#if NET6_0_OR_GREATER
            ChunkBuffer.Write(_buffer.Span.Slice(0, numBytesRead));
#else
            ChunkBuffer.Write(_buffer.ToArray(), 0, numBytesRead);
#endif
            requiredSize -= numBytesRead;
        }

        //Restore the chunk buffer state so that any reads can continue
        ChunkBuffer.Position = storedPosition;

        //No data so stop
        if (ChunkBuffer.Length == 0)
        {
            throw new IOException("Unexpected end of stream, unable to read expected data from the network connection");
        }
    }

    private async ValueTask ReadDataOfSizeAsync(int requiredSize, Memory<byte> data)
    {
        await PopulateChunkBufferAsync(requiredSize).ConfigureAwait(false);
        var readSize = ChunkBuffer.Read(data.Span);

        if (readSize != requiredSize)
        {
            throw new IOException("Unexpected end of stream, unable to read required data size");
        }
    }

    private async ValueTask<bool> ConstructMessageAsync(Stream outputMessageStream)
    {
        var dataRead = false;
        using var borrow = MemoryPool<byte>.Shared.Rent(Constants.ChunkBufferSize * 4);
        var headerBuffer = borrow.Memory.Slice(0, 2);
        
        while (true)
        {
            await ReadDataOfSizeAsync(ChunkHeaderSize, headerBuffer).ConfigureAwait(false);
            var chunkSize = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.Span);

            //NOOP or end of message
            if (chunkSize == 0)
            {
                //We have been reading data so this is the end of a message zero chunk
                //Or there is no data remaining after this NOOP
                if (dataRead || ChunkBufferRemaining <= 0)
                {
                    break;
                }
                //Its a NOOP so skip it
                continue;
            }

            var memory = borrow.Memory.Slice(0, chunkSize);
            await ReadDataOfSizeAsync(chunkSize, memory).ConfigureAwait(false);
            dataRead = true;
            //Put the raw chunk data into the output stream
            // outputMessageStream.Write(memory);
            outputMessageStream.Write(memory.Span.ToArray(), 0, chunkSize);
        }

        //Return if a message was constructed
        return dataRead;
    }
}
