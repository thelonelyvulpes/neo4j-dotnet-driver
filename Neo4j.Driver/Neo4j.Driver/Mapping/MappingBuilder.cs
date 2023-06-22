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
using System.Linq.Expressions;
using System.Reflection;

namespace Neo4j.Driver;

public enum NameStrategy
{
    CamelCase
}

public class MappingBuilder<T> where T : new()
{
    public Mapping<T> Mapping { get; } = new();
    public PropertyInfo[] Properties => typeof(T).GetProperties();
    public FieldInfo[] Fields => typeof(T).GetFields();

    public MappingBuilder<T> Map<TField>(Expression<Func<T, TField>> destination, string sourceKey,
        Func<long, TField> converter)
    {
        var memberExpr = (MemberExpression)destination.Body;
        var settable = Properties
                .Single(x => x.Name == memberExpr.Member.Name)
                .SetMethod
                ?.IsPublic ??
            false;

        var member = Properties.Single(x => x.Name == memberExpr.Member.Name);
        
        return this;
    }

    public MappingBuilder<T> Map<TField>(
        Expression<Func<T, TField>> destination,
        string sourceKey = null,
        Func<string, TField> converter = null)
    {
        var memberExpr = (MemberExpression)destination.Body;
        var settable = Properties
            .Single(x => x.Name == memberExpr.Member.Name)
            .SetMethod
            ?.IsPublic ?? false;

        // Auto mapper suggests not to validate against if a property is settable or not
        // We can use this as a guideline for our own mapping or go the other way
        // https: //github.com/AutoMapper/AutoMapper/issues/1837

        if (!settable)
        {
            throw new ArgumentException("Property is not settable");
        }
        // Mapping.Map(name, (t, l) => map.Compile()(t, convert(l)));
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

    public MappingBuilder<T> Map<TField>(Expression<Func<T, TField>> map)
    {
        return this;
    }
}
