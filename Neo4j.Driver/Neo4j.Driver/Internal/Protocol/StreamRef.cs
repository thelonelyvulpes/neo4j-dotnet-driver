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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;

namespace Neo4j.Driver.Internal;

public sealed class StreamRef : IAsyncDisposable
{
    private readonly StreamDetails _streamDetails;
    private readonly IConnection _socketConnection;
    internal IResponseHandler InitialResponseHandler;
    internal IResponseHandler RecordHandler { get; set; }
    public CancellationToken CancellationToken => cts.Token;

    internal StreamRef(StreamDetails streamDetails, IConnection socketConnection, Action<ContainerToBeRenamed> onRecord)
    {
        _streamDetails = streamDetails;
        _socketConnection = socketConnection;
        InitialResponseHandler = NoOpResponseHandler.Instance;
        RecordHandler = new RecordStreamHandler(onRecord);
        cts = new CancellationTokenSource();
    }

    private readonly CancellationTokenSource cts;
    public async Task Receive()
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                await _socketConnection.ReceiveRecords(this);
            }
        }
        catch (TaskCanceledException)
        {
            // its fine.
        }
    }

    public async Task Stop()
    {
        cts.Cancel();
        await _socketConnection.StopStreamAsync();
    }

    public async ValueTask DisposeAsync()
    {
        cts.Dispose();
        await _socketConnection.CloseAsync();
    }
}

/// <summary>
/// This is bad. I don't know what to do with this.
/// I'm just going to leave this here.
/// </summary>
internal class RecordStreamHandler : IResponseHandler
{
    private readonly Action<ContainerToBeRenamed> _onRecord;

    public RecordStreamHandler(Action<ContainerToBeRenamed> onRecord)
    {
        _onRecord = onRecord;
    }

    public void OnSuccess(IDictionary<string, object> metadata)
    {
    }

    public void OnRecord(object[] fieldValues)
    {
        _onRecord(new ContainerToBeRenamed(fieldValues));
    }

    public void OnFailure(IResponsePipelineError error)
    {
        // yeet.
    }

    public void OnIgnored()
    {
    }
}
