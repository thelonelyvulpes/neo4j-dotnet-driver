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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

public static class Program
{
    private static async Task Main(string[] args)
    {
        var consoleTraceListener = new TextWriterTraceListener(Console.Out);
        Trace.Listeners.Add(consoleTraceListener);
        var cancellation = new CancellationTokenSource();

        try
        {
            var (address, port) = ArgumentsValidation(args);
            using var serverInstance = new TestkitTcpServer(address, port);
            await serverInstance.RunAsync(cancellation.Token);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Unhandled Exception: {ex}");
        }
        finally
        {
            Trace.Flush();
            Trace.Listeners.Remove(consoleTraceListener);
            consoleTraceListener.Close();
            Trace.Close();
        }
    }

    private static (IPAddress address, int port) ArgumentsValidation(string[] args)
    {
        if (args.Length < 2)
        {
            throw new IOException(
                $"Incorrect number of arguments passed in. Expecting Address Port, but got {args.Length} arguments");
        }

        if (!int.TryParse(args[1], out var port) || port < 0)
        {
            throw new IOException(
                $"Invalid port passed in parameter 2.  Should be unsigned integer but was: {args[1]}.");
        }

        if (!IPAddress.TryParse(args[0], out var address))
        {
            throw new IOException($"Invalid IPAddress passed in parameter 1. {args[0]}");
        }

        if (args.Length > 2)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(args[2]));
            Trace.WriteLine("Logging to file: " + args[2]);
        }

        Trace.WriteLine($"Starting TestBackend on {address}:{port}");

        return (address, port);
    }
}
