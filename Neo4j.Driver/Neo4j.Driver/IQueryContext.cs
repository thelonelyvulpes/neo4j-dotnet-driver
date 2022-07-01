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
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>
/// 
/// </summary>
public interface IQueryContext
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="converter"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<IRecordSetResult<T>> QueryAsync<T>(Query query,
        Func<IRecord, T> converter = null);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="parameters"></param>
    /// <param name="converter"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<IRecordSetResult<T>> QueryAsync<T>(string query, object parameters = null,
        Func<IRecord, T> converter = null);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<IRecordSetResult> QueryAsync(Query query, 
        QueryConfig config = null, 
        CancellationToken cancellationToken = default);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    Task<IRecordSetResult> QueryAsync(string query, object parameters = null, 
        Access access = Access.Naive, bool canBeRetried = false);
}

public enum Access
{
    Naive = 0,
    Read = 1,
    Write = 2,
    AutoCommitWrite = 3
}

public sealed class QueryConfig
{
    public static readonly QueryConfig Read = new QueryConfig
    {
        Access = Access.Read
    };
    
    public static readonly QueryConfig Write = new QueryConfig
    {
        Access = Access.Write
    };
    
    public static readonly QueryConfig AutoCommit = new QueryConfig
    {
        Access = Access.AutoCommitWrite
    };

    public Access Access { get; init; } = Access.Naive;
    public int MaxRetry { get; init; } = 2;
    public Func<Exception, int, (bool retry, TimeSpan delay)> RetryFunc { get; init; } = Retries.Transient;
    public string DbName { get; init; } = null;
    
}

public static class Retries
{
    public static Func<Exception, int, (bool retry, TimeSpan delay)> Transient = (ex, n)  => 
        (
            retry: ex is Neo4jException neoEx && neoEx.CanBeRetried, 
            delay: TimeSpan.FromMilliseconds(n * 100 + Random.Shared.Next(-10, 10)) );
}

/// <summary>
/// 
/// </summary>
public interface ITransactionContext
{
    /// <summary>
    /// Asynchronously execute given unit of work as a transaction with a specific <see cref="TransactionConfig"/>.
    /// </summary>
    /// <param name="work">The <see cref="Func{IAsyncQueryRunner, Task}"/> to be applied to a new read transaction.</param>
    /// <param name="action">Given a <see cref="TransactionConfigBuilder"/>, defines how to set the configurations for the new transaction.
    /// This configuration overrides server side default transaction configurations. See <see cref="TransactionConfig"/></param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task<T> ExecuteAsync<T>(Func<IQueryContext, Task<T>> work, Action<TransactionConfigBuilder> action = null);

    /// <summary>
    /// Asynchronously execute given unit of work as a transaction with a specific <see cref="TransactionConfig"/>.
    /// </summary>
    /// <param name="work">The <see cref="Func{IAsyncQueryRunner, Task}"/> to be applied to a new read transaction.</param>
    /// <param name="action">Given a <see cref="TransactionConfigBuilder"/>, defines how to set the configurations for the new transaction.
    /// This configuration overrides server side default transaction configurations. See <see cref="TransactionConfig"/></param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task ExecuteAsync(Func<IQueryContext, Task> work, Action<TransactionConfigBuilder> action = null);
}