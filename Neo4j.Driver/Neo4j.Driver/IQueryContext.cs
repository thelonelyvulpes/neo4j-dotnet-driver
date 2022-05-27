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
    Task<IRecordSetResult<T>> ReadAsync<T>(Query query,
        Func<IRecord, T> converter = null) where T : new();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="parameters"></param>
    /// <param name="converter"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<IRecordSetResult<T>> ReadAsync<T>(string query, object parameters = null,
        Func<IRecord, T> converter = null) where T : new();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="converter"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<IRecordSetResult<T>> WriteAsync<T>(Query query,
        Func<IRecord, T> converter = null) where T : new();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="parameters"></param>
    /// <param name="converter"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<IRecordSetResult<T>> WriteAsync<T>(string query, object parameters = null,
        Func<IRecord, T> converter = null) where T : new();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<IRecordSetResult> ReadAsync(Query query);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    Task<IRecordSetResult> ReadAsync(string query, object parameters = null);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<IRecordSetResult> WriteAsync(Query query);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    Task<IRecordSetResult> WriteAsync(string query, object parameters = null);
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
    Task<T> ReadAsync<T>(Func<IQueryContext, Task<T>> work, Action<TransactionConfigBuilder> action = null);

    /// <summary>
    /// Asynchronously execute given unit of work as a transaction with a specific <see cref="TransactionConfig"/>.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <param name="work">The <see cref="Func{IAsyncQueryRunner, Task}"/> to be applied to a new write transaction.</param>
    /// <param name="action">Given a <see cref="TransactionConfigBuilder"/>, defines how to set the configurations for the new transaction.
    /// This configuration overrides server side default transaction configurations. See <see cref="TransactionConfig"/></param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task<T> WriteAsync<T>(Func<IQueryContext, Task<T>> work, Action<TransactionConfigBuilder> action = null);
    /// <summary>
    /// Asynchronously execute given unit of work as a transaction with a specific <see cref="TransactionConfig"/>.
    /// </summary>
    /// <param name="work">The <see cref="Func{IAsyncQueryRunner, Task}"/> to be applied to a new read transaction.</param>
    /// <param name="action">Given a <see cref="TransactionConfigBuilder"/>, defines how to set the configurations for the new transaction.
    /// This configuration overrides server side default transaction configurations. See <see cref="TransactionConfig"/></param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task ReadAsync(Func<IQueryContext, Task> work, Action<TransactionConfigBuilder> action = null);

    /// <summary>
    /// Asynchronously execute given unit of work as a transaction with a specific <see cref="TransactionConfig"/>.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <param name="work">The <see cref="Func{IAsyncQueryRunner, Task}"/> to be applied to a new write transaction.</param>
    /// <param name="action">Given a <see cref="TransactionConfigBuilder"/>, defines how to set the configurations for the new transaction.
    /// This configuration overrides server side default transaction configurations. See <see cref="TransactionConfig"/></param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    Task WriteAsync(Func<IQueryContext, Task> work, Action<TransactionConfigBuilder> action = null);
}