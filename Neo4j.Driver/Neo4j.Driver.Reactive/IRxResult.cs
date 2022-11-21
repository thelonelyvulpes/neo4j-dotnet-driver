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

namespace Neo4j.Driver;

/// <summary>The reactive result interface</summary>
public interface IRxResult
{
    /// <summary>
    /// Get whether the underlying cursor is open to read records, a cursor will be considered open if
    /// <see cref="Consume"/> has not been called.<br/> Attempting to read records from a closed cursor will throw
    /// <see cref="ResultConsumedException"/>.<br/> Cursors can also be closed if its session is disposed or its session runs a
    /// query.
    /// </summary>
    IObservable<bool> IsOpen { get; }

    /// <summary>
    /// Returns an observable that exposes a single item containing field names returned by the executing query.
    /// Errors raised by actual query execution can surface on the returned observable stream.
    /// </summary>
    /// <returns>An observable stream (with only one element) of field names</returns>
    IObservable<string[]> Keys();

    /// <summary>
    /// Returns an observable that exposes each record returned by the executing query. Errors raised during the
    /// streaming phase can surface on the returned observable stream.
    /// </summary>
    /// <returns>An observable stream of records</returns>
    IObservable<IRecord> Records();

    /// <summary>
    /// Returns an observable that exposes a single item of <see cref="Neo4j.Driver.IResultSummary"/> that is
    /// generated by the server after the streaming of the executing query is completed.
    /// </summary>
    /// 
    /// <remarks>
    /// Subscribing to this stream before subscribing to <see cref="Records"/> causes the results to be discarded on
    /// the server.
    /// </remarks>
    /// <returns>An observable stream (with only one element) of result summary</returns>
    IObservable<IResultSummary> Consume();
}
