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
using System.Linq.Expressions;

namespace Neo4j.Driver;

public class MappingBuilder<T> where T : new()
{
    public Mapping<T> Mapping { get; } = new();

    public MappingBuilder<T> Map<TField>(string name, Expression<Func<T, TField>> map, Func<long, TField> convert)
    {
        // Mapping.Map(name, (t, l) => map.Compile()(t, convert(l)));
        return this;
    }

    public MappingBuilder<T> Map(string name, Action<T, long> map)
    {
        Mapping.Map(name, map);
        return this;
    }

    public MappingBuilder<T> Map(string name, Action<T, string> map)
    {
        Mapping.Map(name, map);
        return this;
    }
    
    public Mapping<T> Build(MapValidationRules rules = null)
    {
        if (rules != null)
        {
            var validation = new MappingValidation<T>();
            validation.Validate(Mapping, rules);
        }
        
        return Mapping;
    }
}
