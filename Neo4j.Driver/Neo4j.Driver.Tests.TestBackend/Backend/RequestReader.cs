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
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal sealed class RequestReader : IDisposable
{
    private static readonly byte[] CloseTag = Encoding.UTF8.GetBytes("\n#request end\n");
    private readonly PipeReader _pipeReader;

    public RequestReader(Stream stream)
    {
        _pipeReader = PipeReader.Create(stream);
    }

    public async Task<IProtocolObject> ParseNextRequest(Action action)
    {
        // Read _pipeReader until we read the open tag
        while (true)
        {
            var readResult = await _pipeReader.ReadAtLeastAsync(16);
            if (readResult is { IsCompleted: true, Buffer.IsEmpty: true })
            {
                await _pipeReader.CompleteAsync();
                throw new FinishedException();
            }

            var buffer = readResult.Buffer;

            var (start, end) = FindIndexes(buffer);
            if (end == -1)
            {
                // We didn't find the end tag, so we need to keep reading
                continue;
            }

            var slice = buffer.Slice(start, end - start);
            var endOfmessage = end + CloseTag.Length;

            var res = ParseProtocolObject(slice,  action);

            _pipeReader.AdvanceTo(buffer.Slice(endOfmessage).Start);
            return res;

        }
    }

    private static IProtocolObject ParseProtocolObject(ReadOnlySequence<byte> slice, Action action)
    {
        string json;
        if (slice.IsSingleSegment)
        {
            json = Encoding.UTF8.GetString(slice.FirstSpan);
        }
        else
        {
            using var memory = MemoryPool<byte>.Shared.Rent((int)slice.Length);
            slice.CopyTo(memory.Memory.Span);
            json = Encoding.UTF8.GetString(memory.Memory.Span);
        }
        Trace.WriteLine($"Received request: {json}.");
        var result = ProtocolObjectFactory.CreateObject(json);
        result.ProtocolEvent = action;
        return result;
    }

    private static (int start, int end) FindIndexes(ReadOnlySequence<byte> buffer)
    {
        const byte openTag = (byte)'{';

        var start = -1;
        var end = -1;
        
        var index = 0;
        var sequenceReader = new SequenceReader<byte>(buffer);
        while(true)
        {
            if (start == -1)
            {
                var span = sequenceReader.CurrentSpan;
                // If we haven't found the start tag yet, we need to find it
                var startIndex = span.IndexOf(openTag);
                if (startIndex == -1)
                {
                    break;
                }
                // We found the start tag
                start = startIndex;
                // }"":value at least should appear before the close tag.
                const int basicDiff = 5;
                index = startIndex + basicDiff;
                sequenceReader.Advance(index);
            } 
            else
            {
                if (sequenceReader.IsNext(CloseTag))
                {
                    end = index;
                    break;
                }

                index++;
                sequenceReader.Advance(1);
                if (sequenceReader.Remaining < CloseTag.Length)
                {
                    break;
                }
            }
        }

        return (start, end);
    }

    public void Dispose()
    {
        _pipeReader.Complete();
    }
}