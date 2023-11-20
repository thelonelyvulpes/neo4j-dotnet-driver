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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

public class Program
{
    private static async Task Main(string[] args)
    {
        await using var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "password"));
        await using var session = driver.AsyncSession(x => x.WithDatabase("neo4j").WithFetchSize(-1));

        const int collected = 1000;
        const int records = 1000;
        
        var tx = await session.BeginTransactionAsync();
        var timers = new List<long>(collected + records);
        var sw = Stopwatch.StartNew();
        var fullSw = Stopwatch.StartNew();
        try
        {
            for (var i = 0; i < collected; i++)
            {
                var cursor = await tx.RunAsync("UNWIND(RANGE(1, 10000)) AS x RETURN collect(toString(x)) as y");
                var rows = await cursor.ToListAsync();

                timers.Add(sw.ElapsedMilliseconds);
                sw.Restart();
                
                if (rows.Count != 1)
                {
                    throw new Exception("Invalid number of rows");
                }
            }

            for (var i = 0; i < records; i++)
            {
                var cursor = await tx.RunAsync("UNWIND(RANGE(1, 10000)) AS x RETURN x");
                var rows = await cursor.ToListAsync();
                timers.Add(sw.ElapsedMilliseconds);
                sw.Restart();
                if (rows.Count != 10_000)
                {
                    throw new Exception("Invalid number of rows");
                }
            }

            await tx.CommitAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await tx.RollbackAsync();
            throw;
        }
        finally
        {
            fullSw.Stop();
        }
        
        Console.WriteLine($"Total time: {fullSw.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average collect time: {timers.Take(collected).Average()}ms");
        Console.WriteLine($"Average record time: {timers.Skip(collected).Take(records).Average()}ms");
    }
}
