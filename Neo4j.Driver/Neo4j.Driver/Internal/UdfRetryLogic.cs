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

namespace Neo4j.Driver.Internal;

internal class UdfRetryLogic : IAsyncRetryLogic
{
    private readonly Func<Exception, int, int, (bool, TimeSpan)> _queryConfigRetryFunc;
    private readonly int _queryConfigMaxRetry;

    public UdfRetryLogic(Func<Exception, int, int, (bool, TimeSpan)> queryConfigRetryFunc, int queryConfigMaxRetry)
    {
        _queryConfigRetryFunc = queryConfigRetryFunc;
        _queryConfigMaxRetry = queryConfigMaxRetry;
    }

    public Task<T> RetryAsync<T>(Func<Task<T>> runTxAsyncFunc)
    {
        return runTxAsyncFunc();
    }
    
    public Task RetryAsync(Func<Task> runTxAsyncFunc)
    {
        return runTxAsyncFunc();
    }
}