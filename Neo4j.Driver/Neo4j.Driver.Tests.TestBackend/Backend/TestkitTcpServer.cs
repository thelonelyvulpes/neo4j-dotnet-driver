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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal sealed class TestkitTcpServer : IDisposable
{
    public TestkitTcpServer(IPAddress address, int port)
    {
        Server = new TcpListener(address, port);
        Server.Start();
        Trace.WriteLine(string.Empty);
    }

    private TcpListener Server { get; }

    private async Task<TcpClient> AwaitConnectionAsync(CancellationToken cancellationToken)
    {
        Trace.WriteLine("Listening for new connection.");
        const int timeout = 1000;
        var connection = await Server.AcceptTcpClientAsync(cancellationToken);
        connection.LingerState.Enabled = false;
        connection.LingerState.LingerTime = 0;
        connection.ReceiveTimeout = timeout;
        var stream = connection.GetStream();
        stream.ReadTimeout = timeout;
        stream.WriteTimeout = timeout;
        return connection;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var conn = await AwaitConnectionAsync(cancellationToken);
            var controller = new Controller(conn.GetStream());

            try
            {
                await controller.Process(true, _ => true);
                Trace.WriteLine("Finished processing.");
            }
            catch (FinishedException ex)
            {
                if (!ex.Healthy)
                {
                    Trace.WriteLine("Unexpected error.");
                    return;
                }
            }
            finally
            {
                Trace.WriteLine("Closing connection.");
                Trace.WriteLine(string.Empty);
            }
        }
    }

    public void Dispose()
    {
        Server.Stop();
    }
}
