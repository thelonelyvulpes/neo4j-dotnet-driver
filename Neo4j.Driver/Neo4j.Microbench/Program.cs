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

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Neo4j.Driver;
using Neo4j.Driver.Internal.Result;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<RecordBenchmarks>();
    }
}

[MemoryDiagnoser]
public class RecordBenchmarks
{
    private readonly (string[], object[])[] _pairs;
    private readonly IRecord[] _dictionaryRecords;
    private readonly IRecord[] _kvpRecords;

    public RecordBenchmarks()
    {
        _pairs = Enumerable
            .Range(1, 4)
            .Select(
                x =>
                    (Enumerable
                            .Range(0, x)
                            .Select(y => y.ToString())
                            .ToArray(),
                        Enumerable
                            .Range(0, x)
                            .Select(y => (object)y).ToArray()))
            .ToArray();

        _kvpRecords = _pairs.Select(x => new KvpRecord(x.Item1, x.Item2)).ToArray();
        _dictionaryRecords = _pairs.Select(x => new Record(x.Item1, x.Item2)).ToArray();
    }

    [Benchmark]
    public IRecord[] Building()
    {
        return _pairs.Select(x => (IRecord)new KvpRecord(x.Item1, x.Item2)).ToArray();
    }
    
    [Benchmark]
    public IRecord[] BuildingDictionary()
    {
        return _pairs.Select(x => (IRecord)new Record(x.Item1, x.Item2)).ToArray();
    }
    
    [Benchmark]
    public object FindingKeyKvp()
    {
        return _kvpRecords.Select((x, y) => x[y.ToString()]).ToArray();
    }
    
    [Benchmark]
    public object FindingKey()
    {
        return _dictionaryRecords.Select((x, y) => x[y.ToString()]).ToArray();
    }
    //
    // [Benchmark]
    // public object IteringKeyKvp()
    // {
    //     return KvpRecords[0].Values.Last();
    // }
    //
    // [Benchmark]
    // public object IteringKey()
    // {
    //     return DictionaryRecords[0].Values.Last();
    // }
}
