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
using System.Collections.Generic;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Internal.Messaging;

internal sealed class BeginStreamMessage : IRequestMessage
{
    internal readonly string fromId;
    public IPackStreamSerializer Serializer => BeginStreamMessageSerializer.Instance;

    public BeginStreamMessage(StreamDetails sd)
    {
        fromId = sd.From;
    }
    
}

internal sealed class BeginStreamMessageSerializer : WriteOnlySerializer
{
    public static readonly BeginStreamMessageSerializer Instance = new();

    private BeginStreamMessageSerializer()
    {
    }

    public override IEnumerable<Type> WritableTypes => new[] { typeof(BeginStreamMessage) };
    public override void Serialize(PackStreamWriter writer, object value)
    {
        if (value is not BeginStreamMessage msg)
        {
            throw new ArgumentException($"Can't serialize message '{value?.GetType().Name}' as {nameof(BeginStreamMessage)}.");
        }
        
        writer.WriteStructHeader(1, 0xFF);
        writer.WriteString(msg.fromId);
    }
}

internal sealed class EndStreamMessage : IRequestMessage
{
    
    public IPackStreamSerializer Serializer => EndStreamMessageSerializer.Instance;
    public static IRequestMessage Instance = new EndStreamMessage();
}

internal class EndStreamMessageSerializer : WriteOnlySerializer
{
    public static readonly EndStreamMessageSerializer Instance = new();

    private EndStreamMessageSerializer()
    {
    }

    public override IEnumerable<Type> WritableTypes => new[] { typeof(EndStreamMessage) };

    public override void Serialize(PackStreamWriter writer, object value)
    {
        writer.WriteStructHeader(0, 0xFE);
    }
}
