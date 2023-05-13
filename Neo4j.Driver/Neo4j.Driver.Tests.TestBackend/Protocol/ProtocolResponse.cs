﻿// Copyright (c) "Neo4j"
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
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal sealed record ProtocolResponse
{
    public ProtocolResponse(string newName, string newId)
    {
        data = new ResponseType();
        name = newName;
        ((ResponseType)data).id = newId;
    }

    public ProtocolResponse(string newName, object dataType)
    {
        name = newName;
        data = dataType;
    }

    public ProtocolResponse(string newName)
    {
        name = newName;
        data = null;
    }

    public string name { get; }
    public object data { get; set; }
    public static readonly ProtocolResponse None = new(null, null);

    public sealed class ResponseType
    {
        public string id { get; set; }
    }

    public ReadOnlySpan<char> Encode()
    {
        return JsonConvert.SerializeObject(this).AsSpan();
    }
}
