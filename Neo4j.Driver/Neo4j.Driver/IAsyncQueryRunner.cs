// Copyright (c) "Neo4j"
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
using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>
///  Common interface for components that can execute Neo4j queries.
/// </summary>
/// <remarks>
/// <see cref="IAsyncSession"/> and <see cref="IAsyncTransaction"/>
/// </remarks>
public interface IAsyncQueryRunner : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// 
    /// Asynchronously run a query and return a task of result stream.
    ///
    /// This method accepts a String representing a Cypher query which will be 
    /// compiled into a query object that can be used to efficiently execute this
    /// query multiple times. This method optionally accepts a set of parameters
    /// which will be injected into the query object query by Neo4j. 
    ///
    /// </summary>
    /// <param name="query">A Cypher query.</param>
    /// <returns>A task of a stream of result values and associated metadata.</returns>
    /// <exception cref="TransactionClosedException">>Thrown when used in a transaction that has previously been closed.</exception>
    Task<IResultCursor> RunAsync(string query);

    /// <summary>
    /// Asynchronously execute a query and return a task of result stream.
    /// </summary>
    /// <param name="query">A Cypher query.</param>
    /// <param name="parameters">A parameter dictionary which is made of prop.Name=prop.Value pairs would be created.</param>
    /// <returns>A task of a stream of result values and associated metadata.</returns>
    /// <exception cref="TransactionClosedException">>Thrown when used in a transaction that has previously been closed.</exception>
    Task<IResultCursor> RunAsync(string query, object parameters);

    /// <summary>
    /// 
    /// Asynchronously run a query and return a task of result stream.
    ///
    /// This method accepts a String representing a Cypher query which will be 
    /// compiled into a query object that can be used to efficiently execute this
    /// query multiple times. This method optionally accepts a set of parameters
    /// which will be injected into the query object query by Neo4j. 
    ///
    /// </summary>
    /// <param name="query">A Cypher query.</param>
    /// <param name="parameters">Input parameters for the query.</param>
    /// <returns>A task of a stream of result values and associated metadata.</returns>
    /// <exception cref="TransactionClosedException">>Thrown when used in a transaction that has previously been closed.</exception>
    Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters);

    /// <summary>
    ///
    /// Asynchronously execute a query and return a task of result stream.
    ///
    /// </summary>
    /// <param name="query">A Cypher query, <see cref="Query"/>.</param>
    /// <returns>A task of a stream of result values and associated metadata.</returns>
    /// <exception cref="TransactionClosedException">>Thrown when used in a transaction that has previously been closed.</exception>
    Task<IResultCursor> RunAsync(Query query);
}


public interface ISummarySession : ISetQueryRunner, ISessionQueryRunner
{
}

public interface ISessionQueryRunner : IResultQueryRunner, IAutoCommitQueryRunner, IAsyncDisposable
{
}

public interface ISetQueryRunner : ISummaryResultQueryRunner, IResultQueryRunner
{
}

public interface ISummaryResultQueryRunner
{
    Task<SetResult<T>> ScalarWithSummaryAsync<T>(Query query, AccessMode access = AccessMode.Read,
        Func<object, T> converter = null);
    Task<SetResult<T>> SingleWithSummaryAsync<T>(Query query, AccessMode access = AccessMode.Read,
        Func<IRecord, T> converter = null) where T : new();
    Task<SetResult<T[]>> QueryWithSummaryAsync<T>(Query query, AccessMode access = AccessMode.Read,
        Func<IRecord, T> converter = null) where T : new();
    Task<SetResult<T>> ScalarWithSummaryAsync<T>(string query, object parameters = null,
        AccessMode access = AccessMode.Read, Func<object, T> converter = null);
    Task<SetResult<T>> SingleWithSummaryAsync<T>(string query, object parameters = null,
        AccessMode access = AccessMode.Read, Func<IRecord, T> converter = null) where T : new();
    Task<SetResult<T[]>> QueryWithSummaryAsync<T>(string query, object parameters = null,
        AccessMode access = AccessMode.Read, Func<IRecord, T> converter = null) where T : new();
    Task<SetResult<object>> ScalarWithSummaryAsync(Query query, AccessMode access = AccessMode.Read);
    Task<SetResult<IRecord>> SingleWithSummaryAsync(Query query, AccessMode access = AccessMode.Read);
    Task<SetResult<IRecord[]>> QueryWithSummaryAsync(Query query, AccessMode access = AccessMode.Read);
    Task<SetResult<object>> ScalarWithSummaryAsync(string query, object parameters = null,
        AccessMode access = AccessMode.Read);
    Task<SetResult<IRecord>> SingleWithSummaryAsync(string query, object parameters = null,
        AccessMode access = AccessMode.Read);
    Task<SetResult<IRecord[]>> QueryWithSummaryAsync(string query, object parameters = null,
        AccessMode access = AccessMode.Read);
}

public interface IResultQueryRunner 
{
    Task<IResultSummary> ExecuteAsync(Query query, AccessMode access = AccessMode.Write);
    Task<object> QueryScalarAsync(Query query, AccessMode access = AccessMode.Read);
    Task<IRecord> QuerySingleAsync(Query query, AccessMode access = AccessMode.Read);
    Task<IRecord[]> QueryAsync(Query query, AccessMode access = AccessMode.Read);
    Task<IResultSummary> ExecuteAsync(string query, object parameters = null, AccessMode access = AccessMode.Write);
    Task<object> QueryScalarAsync(string query, object parameters = null, AccessMode access = AccessMode.Read);
    Task<IRecord> QuerySingleAsync(string query, object parameters = null, AccessMode access = AccessMode.Read);
    Task<IRecord[]> QueryAsync(string query, object parameters = null, AccessMode access = AccessMode.Read);
    Task<T> QueryScalarAsync<T>(Query query, AccessMode access = AccessMode.Read, Func<object, T> converter = null);
    Task<T> QuerySingleAsync<T>(Query query, AccessMode access = AccessMode.Read, Func<IRecord, T> converter = null)
        where T : new();
    Task<T[]> QueryAsync<T>(Query query, AccessMode access = AccessMode.Read, Func<IRecord, T> converter = null)
        where T : new();
    Task<T> QueryScalarAsync<T>(string query, object parameters = null, AccessMode access = AccessMode.Read,
        Func<object, T> converter = null);
    Task<T> QuerySingleAsync<T>(string query, object parameters = null, AccessMode access = AccessMode.Read,
        Func<IRecord, T> converter = null) where T : new();
    Task<T[]> QueryAsync<T>(string query, object parameters = null, AccessMode access = AccessMode.Read,
        Func<IRecord, T> converter = null) where T : new();
}

public interface IAutoCommitQueryRunner
{
    Task<IResultCursor> RunInAutoCommitAsync(Query query);
    Task<IResultCursor> RunInAutoCommitAsync(string query, object parameters = null);
}

public class SetResult<T>
{
    public T Results { get; internal set; }
    public IResultSummary Summary { get; internal set; }
}
