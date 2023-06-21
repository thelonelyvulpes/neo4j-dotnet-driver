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

namespace Neo4j.Driver;

public class Mapping<T> : IMapping where T : new()
{
    public Type Type { get; } = typeof(T);
    internal Dictionary<string, List<Type>> RegisteredKeys { get; } = new();
    public Dictionary<string, Action<T, double>> DoubleMaps { get; } = new();
    public Dictionary<string, Action<T, long>> LongMaps { get; } = new();
    public Dictionary<string, Action<T, string>> StringMaps { get; } = new();

    public void Map(string name, Action<T, double> map)
    {
        if (RegisteredKeys.TryGetValue(name, out var value))
        {
            value.Add(typeof(double));
        }
        else
        {
            RegisteredKeys.Add(name, new List<Type> { typeof(double) });
        }
        
        DoubleMaps.Add(name, map);
    }

    public void Map(string name, Action<T, long> map)
    {
        if (RegisteredKeys.TryGetValue(name, out var value))
        {
            value.Add(typeof(long));
        }
        else
        {
            RegisteredKeys.Add(name, new List<Type> { typeof(long) });
        }
        
        LongMaps.Add(name, map);
    }

    public void Map(string name, Action<T, string> map)
    {
        if (RegisteredKeys.TryGetValue(name, out var value))
        {
            value.Add(typeof(string));
        }
        else
        {
            RegisteredKeys.Add(name, new List<Type> { typeof(string) });
        }
        
        StringMaps.Add(name, map);
    }
}