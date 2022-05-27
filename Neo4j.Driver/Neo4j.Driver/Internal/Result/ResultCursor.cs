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
using System.Linq;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Result
{
    internal class ResultCursor : IInternalResultCursor
    {
        private bool _atEnd;
        private IRecord _peeked;
        private IRecord _current;

        private Task<string[]> _keys;
        private readonly IResultStream _resultStream;
        private Task<IResultSummary> _summary;

        public ResultCursor(IResultStream resultStream)
        {
            Throw.ArgumentNullException.IfNull(resultStream, nameof(resultStream));

            _resultStream = resultStream;
        }

        public Task<string[]> KeysAsync()
        {
            if (_keys == null)
            {
                _keys = _resultStream.GetKeysAsync();
            }

            return _keys;
        }

        public Task<IResultSummary> ConsumeAsync()
        {
            if (_summary == null)
            {
                Cancel();
                _summary = _resultStream.ConsumeAsync();
            }
            else
            {
                if (_summary.IsFaulted)
                {
                    _summary = _resultStream.ConsumeAsync();
                }
            }

            return _summary;
        }

        public async Task<IRecord> PeekAsync()
        {
            if (_peeked != null)
            {
                return _peeked;
            }

            if (_atEnd)
            {
                return null;
            }

            _peeked = await _resultStream.NextRecordAsync().ConfigureAwait(false);
            if (_peeked == null)
            {
                _atEnd = true;

                return null;
            }

            return _peeked;
        }

        public async Task<bool> FetchAsync()
        {
            if (_peeked != null)
            {
                _current = _peeked;
                _peeked = null;
            }
            else
            {
                try
                {
                    _current = await _resultStream.NextRecordAsync().ConfigureAwait(false);
                }
                finally
                {
                    if (_current == null)
                    {
                        _atEnd = true;
                    }
                }
            }

            return _current != null;
        }

        public IRecord Current
        {
            get
            {
                if (!_atEnd && _current == null && _peeked == null)
                    throw new InvalidOperationException("Tried to access Current without calling FetchAsync.");

                return _current;
            }
        }

        public bool IsOpen => _summary == null;
        public async Task<IRecordSetResult> ToResultAsync()
        {
            var rows = await this.ToListAsync().ConfigureAwait(false);
            var keys = await KeysAsync().ConfigureAwait(false);
            var summary = await ConsumeAsync().ConfigureAwait(false);
            return new InternalRecordSetResult
            {
                Keys = keys,
                Results = rows.ToArray(),
                Summary = summary
            };
        }

        public async Task<IRecordSetResult<T>> ToResultAsync<T>(Func<IRecord, T> converter = null)
        {
            var set = new List<T>();
            while (await FetchAsync().ConfigureAwait(false))
                set.Add(converter(_current));
            
            var keys = await KeysAsync().ConfigureAwait(false);
            var summary = await ConsumeAsync().ConfigureAwait(false);
            return new InternalRecordSetResult<T>
            {
                Keys = keys,
                Results = set.ToArray(),
                Summary = summary
            };
        }

        public void Cancel()
        {
            _resultStream.Cancel();
        }
    }
}