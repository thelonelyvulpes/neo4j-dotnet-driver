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
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

public class Program
{
    private static IPAddress Address;
    private static uint Port;

    private static async Task Main(string[] args)
    {
        var consoleTraceListener = new TextWriterTraceListener(Console.Out);
        Trace.Listeners.Add(consoleTraceListener);

        await using var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.None);
        await using (var session = driver.AsyncSession(x => x.WithDatabase("system")))
        {
            var cursor = await session.RunAsync("CREATE DATABASE cdctest OPTIONS {txLogEnrichment: \"FULL\"}");
            await cursor.ConsumeAsync();
        }
        
        await Task.Delay(2000);

        await using (var session = driver.AsyncSession(x => x.WithDatabase("cdctest")))
        {
            var cursor = await session.RunAsync("CREATE (:LAVEL {x: $i})", new { i = -1 });
            await cursor.ConsumeAsync();
        }

        List<IRecord> cdcResult;
        await using (var session = driver.AsyncSession(x => x.WithDatabase("cdctest")))
        {
            var cursor = await session.RunAsync("CALL cdc.earliest()");
            cdcResult = await cursor.ToListAsync();
        }

        var streamDetails = new StreamDetails
        {
            From = cdcResult[0]["id"].As<string>(),
        };
        
        Console.WriteLine($"Starting {streamDetails.From}");
        
        await using var stream = await driver.OpenCdcStreamAsync(
            streamDetails,
            x =>
            {
                Console.WriteLine("Received CDC: record");
                Console.WriteLine(JsonConvert.SerializeObject(x, Formatting.Indented));
            });
        
        var receive = stream.Receive();
        Console.WriteLine("Opened stream");
        var i = 0;
        while (true)
        {
            var input = Console.ReadLine();
            if (input == "c")
            {
                Console.WriteLine("Stopping Stream");
                await stream.Stop();
                break;
            }
            
            Console.WriteLine("Sending record");
            await using (var session = driver.AsyncSession(x => x.WithDatabase("cdctest")))
            {
                var cursor = await session.RunAsync("CREATE (:LAVEL {x: $i})", new { i });
                await cursor.ConsumeAsync();
            }
            i++;
        }

        Console.WriteLine("Fin.");
    }
}
