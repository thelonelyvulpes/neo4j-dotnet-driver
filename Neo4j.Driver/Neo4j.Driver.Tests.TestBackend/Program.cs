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

        await using var driver =
            GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.None, x => x.WithLogger(new SimpleLogger()));

        List<IRecord> cdcResult;
        await using (var session = driver.AsyncSession(x => x.WithDatabase("cdctest")))
        {
            var cursor = await session.RunAsync("CALL cdc.current()");
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
                Console.WriteLine("recv");
                recvCounter++;
            });
        
        var receive = stream.Receive();
        Console.WriteLine("Opened stream");
        await using (var session = driver.AsyncSession(x => x.WithDatabase("cdctest")))
        {
            while (i < 3)
            {
                var cursor = await session.RunAsync(
                    // "UNWIND [0, 1, 2, 3, 4, 5, 6, 7, 8, 9] as i " +
                    "CREATE (:LABEL {x: $i})", new { i });
                await cursor.ConsumeAsync();
                i++;
            }
        }
        
        await Task.Delay(1000);
        Console.WriteLine("Stopping stream");
        await stream.Stop();
        Console.WriteLine($"Received {recvCounter} records");
        Console.WriteLine("Fin.");
    }

    private static int i;
    static int recvCounter = 0;
}
