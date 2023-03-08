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
using System.Linq;
using System.Runtime.InteropServices;

namespace Neo4j.Driver.Internal.Result;

internal sealed class KvpRecord : IRecord
{
    private readonly KvpSet _kvp;

    public KvpRecord(string[] keys, object[] values)
    {
        _kvp = new KvpSet(keys, values);
    }

    public object this[int index] => _kvp[index];

    public object this[string key] => _kvp[key];

    public IReadOnlyDictionary<string, object> Values => _kvp;
    public IReadOnlyList<string> Keys => _kvp.KeyArray;
}

internal sealed record KvpSet : IReadOnlyDictionary<string, object>
{
    public KvpSet(string[] keyArray, object[] valueArray)
    {
        ValueArray = valueArray;
        KeyArray = keyArray;
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return new KeyValuePair<string, object>(KeyArray[i], ValueArray[i]);
        }
    }

    internal readonly object[] ValueArray;
    internal readonly string[] KeyArray;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => ValueArray.Length;

    public bool ContainsKey(string key)
    {
        var index = Array.IndexOf(KeyArray, key);
        return index > -1;
    }

    public bool TryGetValue(string key, out object value)
    {
        var index = Array.IndexOf(KeyArray, key);
        if (index > -1)
        {
            value = ValueArray[index];
            return true;
        }

        value = null;
        return false;
    }

    public object this[int index] => ValueArray[index];

    public object this[string key]
    {
        get
        {
#if NET6_0_OR_GREATER
            var span = new Span<string>(KeyArray);
            for (var i = 0; i < span.Length; i++)
            {
                if (span[i].Equals(key, StringComparison.Ordinal))
                {
                    return ValueArray[i];
                }
            }
#else
           for (var i = 0; i < KeyArray.Length; i++)
            {
                if (KeyArray[i].Equals(key, StringComparison.Ordinal))
                {
                    return ValueArray[i];
                }
            }
#endif

            return null;
        }
    }

    public IEnumerable<string> Keys => KeyArray;
    public IEnumerable<object> Values => ValueArray;
}
