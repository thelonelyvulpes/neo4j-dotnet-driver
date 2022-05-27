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
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Result
{
    internal class ConsumableResultCursor : IInternalResultCursor
    {
        private readonly IInternalResultCursor _cursor;
        private bool _isConsumed;

        public ConsumableResultCursor(IInternalResultCursor cursor)
        {
            _cursor = cursor;
        }

        public Task<string[]> KeysAsync()
        {
            return _cursor.KeysAsync();
        }

        public Task<IResultSummary> ConsumeAsync()
        {
            _isConsumed = true;
            return _cursor.ConsumeAsync();
        }

        public Task<IRecord> PeekAsync()
        {
            AssertNotConsumed();
            return _cursor.PeekAsync();
        }

        public Task<bool> FetchAsync()
        {
            AssertNotConsumed();
            return _cursor.FetchAsync();
        }

        public IRecord Current
        {
            get
            {
                AssertNotConsumed();
                return _cursor.Current;
            }
        }

        public bool IsOpen => !_isConsumed;
        public Task<IRecordSetResult> ToResultAsync()
        {
            return _cursor.ToResultAsync();
        }

        public Task<IRecordSetResult<T>> ToResultAsync<T>(Func<IRecord, T> converter = null)
        {
            return _cursor.ToResultAsync(converter);
        }

        public void Cancel()
        {
            _cursor.Cancel();
        }

        private void AssertNotConsumed()
        {
            if (_isConsumed)
            {
                throw ErrorExtensions.NewResultConsumedException();
            }
        }
    }

    internal class InternalRecordSetResult : IRecordSetResult
    {
        public IRecord[] Results { get; internal set; }
        public IResultSummary Summary { get; internal set; }
        public string[] Keys { get; internal set; }
    }

    internal class InternalRecordSetResult<T> : IRecordSetResult<T>
    {
        public T[] Results { get; internal set; }
        public IResultSummary Summary { get; internal set; }
        public string[] Keys { get; internal set; }
    }
}