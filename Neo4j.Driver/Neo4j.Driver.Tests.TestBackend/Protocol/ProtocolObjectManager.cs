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

using System.Collections.Generic;

namespace Neo4j.Driver.Tests.TestBackend;

internal sealed class ProtocolObjectManager
{
    private static int ObjectCounter { get; set; }
    private Dictionary<string, IProtocolObject> ProtocolObjects { get; } = new();

    public static string GenerateUniqueIdString()
    {
        return (ObjectCounter++).ToString();
    }

    public void AddProtocolObject(IProtocolObject obj)
    {
        obj.SetUniqueId(GenerateUniqueIdString());
        ProtocolObjects[obj.uniqueId] = obj;
    }

    public IProtocolObject GetObject(string id)
    {
        return string.IsNullOrEmpty(id) ? null : ProtocolObjects[id];
    }

    public T GetObject<T>(string id) where T : IProtocolObject
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return (T)ProtocolObjects[id];
    }
}
