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

public sealed class StreamRef : IAsyncDisposable, IAsyncEnumerable<ContainerToBeRenamed>
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
        OnRecord = onRecord;
        RecordHandler = new RecordStreamHandler(ProcessRecord, OnSuccess);
        cts = new CancellationTokenSource();
        PullMore = false;
    }

    internal StreamRef(StreamDetails streamDetails, IConnection socketConnection)
    {
        _streamDetails = streamDetails;
        _socketConnection = socketConnection;
        InitialResponseHandler = NoOpResponseHandler.Instance;
        RecordHandler = new RecordStreamHandler(ProcessRecord, OnSuccess);
        cts = new CancellationTokenSource();
        PullMore = false;
    }

    private void ProcessRecord(ContainerToBeRenamed obj)
    {
        OnRecord(obj);
        LastId = obj.ToString();
    }

    public Action<ContainerToBeRenamed> OnRecord { get; set; }

    private readonly CancellationTokenSource cts;
    public async Task<string> Receive(CancellationToken ct)
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                var p = PullMore;
                PullMore = false;
                await _socketConnection.ReceiveRecords(this, p);
            }
        }
        catch (TaskCanceledException)
        {
            // its fine.
        }

        return LastId;
    }
    
    public async Task<string> ReceiveAsync(Action<ContainerToBeRenamed> onRecord, CancellationToken ct)
    {
        OnRecord = onRecord;   
        try
        {
            while (!cts.IsCancellationRequested)
            {
                var p = PullMore;
                PullMore = false;
                await _socketConnection.ReceiveRecords(this, p);
            }
        }
        catch (TaskCanceledException)
        {
            // its fine.
        }

        return LastId;
    }

    private void OnSuccess(IDictionary<string, object> metadata)
    {
        if (metadata.TryGetValue("has_more", out var hasMore) && hasMore is true)
            PullMore = true;
    }

    public bool PullMore { get; set; }
    public string LastId { get; set; }

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

    public IAsyncEnumerable<ContainerToBeRenamed> AsyncIterate()
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerator<ContainerToBeRenamed> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// This is bad. I don't know what to do with this.
/// I'm just going to leave this here.
/// </summary>
internal class RecordStreamHandler : IResponseHandler
{
    private readonly Action<ContainerToBeRenamed> _onRecord;
    private readonly Action<IDictionary<string, object>> _onSuccess;

    public RecordStreamHandler(Action<ContainerToBeRenamed> onRecord, Action<IDictionary<string, object>> onSuccess)
    {
        _onRecord = onRecord;
        _onSuccess = onSuccess;
    }

    public void OnSuccess(IDictionary<string, object> metadata)
    {
        _onSuccess(metadata);
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
