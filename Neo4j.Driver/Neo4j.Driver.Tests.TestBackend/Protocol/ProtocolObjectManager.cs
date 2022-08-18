﻿// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

internal class ProtocolObjectManager
{
    private static int ObjectCounter { get; set; }
    private Dictionary<string, ProtocolObject> ProtocolObjects { get; } = new();
    public int ObjectCount => ProtocolObjects.Count;

    public static string GenerateUniqueIdString()
    {
        return (ObjectCounter++).ToString();
    }

    public static int GenerateUniqueIdInt()
    {
        return ObjectCounter++;
    }

    public void AddProtocolObject(ProtocolObject obj)
    {
        obj.UniqueId = GenerateUniqueIdString();
        ProtocolObjects[obj.UniqueId] = obj;
    }

    public T GetObject<T>(string id) where T : ProtocolObject
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return (T) ProtocolObjects[id];
    }
}