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
using System.Collections;
using System.Collections.Generic;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Internal;

internal class AttachMessageSerializer : WriteOnlySerializer
{
    public override IEnumerable<Type> WritableTypes => new[] { typeof(AttachMessage) };
    public static readonly AttachMessageSerializer Instance = new();

    public override void Serialize(PackStreamWriter writer, object value)
    {
        var msg = value.CastOrThrow<AttachMessage>();
        writer.WriteStructHeader(1, MessageFormat.MsgAttachSession);
        writer.WriteDictionary(new Dictionary<string, object>()
        {
            ["sid"] = msg.SessionId
        } as IDictionary<string, object>);
    }
}
