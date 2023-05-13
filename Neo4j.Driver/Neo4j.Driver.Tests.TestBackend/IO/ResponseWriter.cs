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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal sealed class ResponseWriter
{
    private static readonly ReadOnlyMemory<byte> OpenTag = new(Encoding.UTF8.GetBytes("#response begin\n"));
    private static readonly ReadOnlyMemory<byte> CloseTag = new(Encoding.UTF8.GetBytes("\n#response end\n"));

    private readonly PipeWriter _pipeWriter;
    private readonly JsonSerializerOptions _serializerSettings;

    public ResponseWriter(Stream writer)
    {
        _pipeWriter = PipeWriter.Create(writer);
        _serializerSettings = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
    }

    public Task WriteResponseAsync(IProtocolObject protocolObject)
    {
        return WriteResponseAsync(protocolObject.Response());
    }

    public Task WriteResponseAsync(ProtocolResponse response)
    {
        if (response == ProtocolResponse.None)
        {
            Trace.WriteLine("Noop response.\n");
            return Task.CompletedTask;
        }
        var responseString = response.Encode();
        Trace.WriteLine($"Sending response: {responseString}\n");
        _pipeWriter.Write(OpenTag.Span);
        // using var _jsonWriter = new Utf8JsonWriter(_pipeWriter);
        // JsonSerializer.Serialize(_jsonWriter, response, _serializerSettings);
        using var memory = MemoryPool<byte>.Shared.Rent(responseString.Length);
        Encoding.UTF8.GetBytes(responseString, memory.Memory.Span);
        _pipeWriter.Write(memory.Memory.Slice(0, responseString.Length).Span);
        _pipeWriter.Write(CloseTag.Span);
        return _pipeWriter.FlushAsync().AsTask();
    }

    public Task WriteResponseAsync(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            return Task.CompletedTask;
        }

        Trace.WriteLine($"Sending response: {response}\n");
        
        using var memory = MemoryPool<byte>.Shared.Rent(response.Length);
        Encoding.UTF8.GetBytes(response, memory.Memory.Span);
        _pipeWriter.Write(OpenTag.Span);
        _pipeWriter.Write(memory.Memory.Slice(0, response.Length).Span);
        _pipeWriter.Write(CloseTag.Span);
        return _pipeWriter.FlushAsync().AsTask();
    }
}
