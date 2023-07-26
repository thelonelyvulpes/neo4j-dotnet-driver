﻿// Copyright (c) "Neo4j"
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
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

public class Program
{
    private static IPAddress Address;
    private static uint Port;

    private static int i;
    private static int recvCounter = 0;

    private static async Task Main(string[] args)
    {
        var consoleTraceListener = new TextWriterTraceListener(Console.Out);
        Trace.Listeners.Add(consoleTraceListener);

        await using var driver =
            GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.None, x => x.WithLogger(new MySimpleLogger()));

        List<IRecord> cdcResult;
        await using (var session = driver.AsyncSession(x => x.WithDatabase("cdctest")))
        {
            var cursor = await session.RunAsync("CALL cdc.current()");
            cdcResult = await cursor.ToListAsync();
        }

        var firstId = cdcResult[0]["id"].As<string>();

        await StreamIter(firstId, driver);

        await StreamLambda(firstId, driver);
    }

    private static async Task StreamIter(string firstId, IDriver driver, CancellationToken cancellationToken = default)
    {
        var streamDetails = new StreamDetails
        {
            From = firstId
        };

        await using var stream = await driver.OpenStreamAsync(streamDetails);
        await foreach (var record in stream)
        { // set lsat id
            
        }

        return stream.LastId;
        // can access lastId
    }

    public static void Stream(string fromId, IDriver driver, CancellationToken cancellationToken)
    {
        var streamDetails = new StreamDetails
        {
            From = fromId,
            CancellationToken = cancellationToken
        };
        
        using var session = driver.Session(x => x.WithDatabase("cdc"));
        using var handle = session.CreateCdcHandle(streamDetails);
        foreach (var record in (IEnumerable)handle)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            
        }
    }

    private static async Task StreamLambda(string firstId, IDriver driver, CancellationToken cancellationToken = default)
    {
        var streamDetails = new StreamDetails
        {
            From = firstId,
        };
    
        await using var stream = await driver.OpenStreamAsync(streamDetails);
        
        var lastToken = await stream.ReceiveAsync(
            record =>
            {
            },
            cancellationToken);
    }

    private static async Task StreamFluent(
        string firstId,
        IDriver driver,
        CancellationToken cancellationToken = default)
    {
        var streamReport = await driver.ChangeStream("mydb", "mychange")
            .WithSelector(new ChangeSelector())
            .StreamProcessor(async iter =>
                {
                    await foreach (var record in iter.WithCancellation(cancellationToken))
                    {
                        Console.WriteLine(record);
                    }
                })
            .WithAutoRecovery()
            .ExecuteAsync(cancellationToken);
        
        // See where we started and got to
        Console.WriteLine(streamReport.FirstToken);
        Console.WriteLine(streamReport.LastToken);
        Console.WriteLine(streamReport.TotalRecordsReceived);
        // Know how many times we had to reset connection
    }
}

internal class MySimpleLogger : SimpleLogger
{
    public override void Trace(string message, params object[] args)
    {
    }

    public override bool IsTraceEnabled()
    {
        return false;
    }
}
