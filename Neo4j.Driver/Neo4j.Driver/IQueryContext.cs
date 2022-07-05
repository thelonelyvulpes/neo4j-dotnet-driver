// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

namespace Neo4j.Driver;

public interface IQueryContext
{
    Task<IRecordSetResult> QueryAsync(Query query, 
        QueryConfig config = null, 
        CancellationToken cancellationToken = default);

    Task<IRecordSetResult<T>> QueryAsync<T>(Query query,
        QueryConfig<T> config = null,
        CancellationToken cancellationToken = default);

    Task<IRecordSetResult> QueryAsync(string query, 
        object parameters = null, 
        ClusterMemberAccess clusterMemberAccess = ClusterMemberAccess.Naive,
        int maxRetry = 0,
        string dbName = null,
        string impersonation = null,
        Bookmarks bookmarks = null,
        CancellationToken cancellationToken = default);

    Task<IRecordSetResult<T>> QueryAsync<T>(string query,
        object parameters = null,
        ClusterMemberAccess clusterMemberAccess = ClusterMemberAccess.Naive,
        Func<IRecord, T> mapper = null,
        int maxRetry = 0,
        string dbName = null,
        string impersonation = null,
        Bookmarks bookmarks = null,
        CancellationToken cancellationToken = default);

    Task<IRecordSetResult> QueryAsync(string query,
        Dictionary<string, object> parameters = null,
        ClusterMemberAccess clusterMemberAccess = ClusterMemberAccess.Naive,
        int maxRetry = 0,
        string dbName = null,
        string impersonation = null,
        Bookmarks bookmarks = null,
        CancellationToken cancellationToken = default);

    Task<IRecordSetResult<T>> QueryAsync<T>(string query,
        Dictionary<string, object> parameters = null,
        ClusterMemberAccess clusterMemberAccess = ClusterMemberAccess.Naive,
        Func<IRecord, T> mapper = null,
        int maxRetry = 0,
        string dbName = null,
        string impersonation = null,
        Bookmarks bookmarks = null,
        CancellationToken cancellationToken = default);
}

public enum ClusterMemberAccess
{
    Naive = 0,
    Follower = 1,
    Leader = 2
}

public enum TransactionClusterMemberAccess
{
    Follower = 1,
    Leader = 2
}

public class QueryConfig
{
    public static readonly QueryConfig Read = new QueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Follower
    };
    
    public static readonly QueryConfig Write = new QueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Leader
    };
    public static readonly QueryConfig AutoCommit = new QueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Leader,
        MaxRetry = 0
    };

    public ClusterMemberAccess ClusterMemberAccess { get; set; } = ClusterMemberAccess.Naive;
    public int MaxRetry { get; set; } = 2;
    public Func<Exception, int, (bool retry, TimeSpan delay)> RetryFunc { get; set; } = Retries.Transient;
    public string DbName { get; set; } = null;
    public Dictionary<string, string> Metadata { get; set; }
    public TimeSpan Timeout { get; set; }
    public Bookmarks Bookmarks { get; set; } = null;
}


public class QueryConfig<T> : QueryConfig
{
    public Func<IRecord, T> Mapper { get; set; } = null;
}

public class TxConfig
{
    public int MaxRetry { get; set; } = 2;
    public Func<Exception, int, (bool retry, TimeSpan delay)> RetryFunc { get; set; } = Retries.Transient;
    public string DbName { get; set; } = null;
    public Dictionary<string, string> Metadata { get; set; }
    public Bookmarks Bookmarks { get; set; } = null;
    public TimeSpan Timeout { get; set; }
}

public static class Retries
{
    public static Func<Exception, int, (bool retry, TimeSpan delay)> Transient = (ex, n)  => 
        (retry: ex is Neo4jException neoEx && neoEx.CanBeRetried, 
            delay: TimeSpan.FromMilliseconds(n * 100 + new Random().Next(-10, 10)));
}

/// <summary>
/// 
/// </summary>
public interface ITransactionContext
{
    Task ExecuteAsync(Func<IQueryContext, Task> work, TransactionClusterMemberAccess clusterMemberAccess, TxConfig config = null);
    Task<T> ExecuteAsync<T>(Func<IQueryContext, Task<T>> work, TransactionClusterMemberAccess clusterMemberAccess, TxConfig config = null);
}