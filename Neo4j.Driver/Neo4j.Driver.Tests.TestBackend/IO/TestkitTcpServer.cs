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
        Trace.WriteLine("Creating Server");
        Server = new TcpListener(address, port);
        Server.Start();
        Trace.WriteLine("Server Created");
    }

    private TcpListener Server { get; }

    public NetworkStream ConnectionStream { get; set; }

    public void Dispose()
    {
        ConnectionStream?.Dispose();
    }

    private async Task<TcpClient> AwaitConnectionAsync(CancellationToken cancellationToken)
    {
        const int timeout = 1000;
        Trace.WriteLine("Listening for connection");
        var connection = await Server.AcceptTcpClientAsync(cancellationToken);
        connection.LingerState.Enabled = false;
        connection.LingerState.LingerTime = 0;
        connection.ReceiveTimeout = timeout;
        var stream = connection.GetStream();
        stream.ReadTimeout = timeout;
        stream.WriteTimeout = timeout;
        Trace.WriteLine("Connected");
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
                await controller.Process(_ => true, cancellationToken);
            }
            catch (FinishedException)
            {
                // Ignore, this is expected.
            }

            Trace.WriteLine("Finished processing");
        }
    }
}
