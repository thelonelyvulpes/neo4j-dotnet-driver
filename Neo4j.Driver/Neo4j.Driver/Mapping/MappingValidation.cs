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
using System.Linq;

namespace Neo4j.Driver;

public class MappingValidation<T> where T : new()
{
    public bool Validate(Mapping<T> mapping, MapValidationRules rules)
    {
        var type = mapping.Type;

        if (!rules.AllowNonMappedFields)
        {
            ValidateFields(mapping, type);
        }
        
        if (!rules.AllowNonMappedProperties)
        {
            ValidateProperties(mapping, type);
        }

        return true;
    }

    private static void ValidateProperties(Mapping<T> mapping, Type type)
    {
        var props = type.GetProperties().Where(x => x.SetMethod?.IsPublic ?? false).ToList();
        if (props.All(x => mapping.RegisteredKeys.ContainsKey(x.Name)))
        {
            throw new Exception("All properties must be mapped");
        }
    }

    private static void ValidateFields(Mapping<T> mapping, Type type)
    {
        var fields = type.GetFields().Where(x => x.IsPublic).ToList();
        if (fields.All(x => mapping.RegisteredKeys.ContainsKey(x.Name)))
        {
            throw new Exception("All fields must be mapped");
        }
    }
}
