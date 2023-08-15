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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>Extension methods for <see cref="IResultCursor"/></summary>
public static class ResultCursorExtensions
{
    /// <summary>Return the only record in the result stream.</summary>
    /// <param name="result">The result stream</param>
    /// <returns>The only record in the result stream.</returns>
    /// <remarks>
    /// Throws <exception cref="InvalidOperationException"></exception> if the result contains more than one record or
    /// the result is empty.
    /// </remarks>
    public static async Task<IRecord> SingleAsync(this IResultCursor result)
    {
        result = result ?? throw new ArgumentNullException(nameof(result));

        var iter = result.GetAsyncEnumerator();
        if (!await iter.MoveNextAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("The result is empty.");
        }

        var record = result.Current;
        if (!await iter.MoveNextAsync().ConfigureAwait(false))
        {
            return record;
        }

        throw new InvalidOperationException("The result contains more than one element.");

    }

    /// <summary>Return the only record in the result stream.</summary>
    /// <param name="result">The result stream</param>
    /// <param name="operation">The operation to carry out on each record.</param>
    /// <typeparam name="T">The type of the record after specified operation.</typeparam>
    /// <returns>The only record after specified operation in the result stream.</returns>
    /// <remarks>
    /// Throws <exception cref="InvalidOperationException"></exception> if the result contains more than one record or
    /// the result is empty.
    /// </remarks>
    public static async Task<T> SingleAsync<T>(this IResultCursor result, Func<IRecord, T> operation)
    {
        result = result ?? throw new ArgumentNullException(nameof(result));

        var iter = result.GetAsyncEnumerator();
        if (!await iter.MoveNextAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("The result is empty.");
        }

        var record = result.Current;
        if (!await iter.MoveNextAsync().ConfigureAwait(false))
        {
            return operation(record);
        }

        throw new InvalidOperationException("The result contains more than one element.");

    }

    /// <summary>Pull all records in the result stream into memory and return in a list.</summary>
    /// <param name="result"> The result stream.</param>
    /// <returns>A list with all records in the result stream.</returns>
    public static async Task<List<IRecord>> ToListAsync(this IResultCursor result)
    {
        result = result ?? throw new ArgumentNullException(nameof(result));
        var list = new List<IRecord>();
        var iter = result.GetAsyncEnumerator();
        while (await iter.MoveNextAsync().ConfigureAwait(false))
        {
            list.Add(result.Current);
        }

        return list;
    }
    

    /// <summary>Pull all records in the result stream into memory and return in a list.</summary>
    /// <param name="result"> The result stream.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A list with all records in the result stream.</returns>
    public static async Task<List<IRecord>> ToListAsync(this IResultCursor result, CancellationToken cancellationToken)
    {
        result = result ?? throw new ArgumentNullException(nameof(result));
        var list = new List<IRecord>();
        var iter = result.GetAsyncEnumerator(cancellationToken);
        while (await iter.MoveNextAsync().ConfigureAwait(false))
        {
            list.Add(result.Current);
        }

        return list;
    }


    /// <summary>Apply the operation on each record in the result stream and return the operation results in a list.</summary>
    /// <typeparam name="T">The return type of the list</typeparam>
    /// <param name="result">The result stream.</param>
    /// <param name="operation">The operation to carry out on each record.</param>
    /// <returns>A list of collected operation result.</returns>
    public static async Task<List<T>> ToListAsync<T>(this IResultCursor result, Func<IRecord, T> operation)
    {
        result = result ?? throw new ArgumentNullException(nameof(result));
        var list = new List<T>();
        var iter = result.GetAsyncEnumerator();
        while (await iter.MoveNextAsync().ConfigureAwait(false))
        {
            var record = result.Current;
            list.Add(operation(record));
        }

        return list;
    }

    /// <summary>Read each record in the result stream and apply the operation on each record.</summary>
    /// <param name="result">The result stream.</param>
    /// <param name="operation">The operation is carried out on each record.</param>
    /// <returns>The result summary after all records have been processed.</returns>
    public static async Task<IResultSummary> ForEachAsync(
        this IResultCursor result,
        Action<IRecord> operation)
    {
        result = result ?? throw new ArgumentNullException(nameof(result));
        var iter = result.GetAsyncEnumerator();
        while (await iter.MoveNextAsync().ConfigureAwait(false))
        {
            var record = result.Current;
            operation(record);
        }

        return await result.ConsumeAsync().ConfigureAwait(false);
    }
}
