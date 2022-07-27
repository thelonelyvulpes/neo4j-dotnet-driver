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
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Result;
using static Neo4j.Driver.Internal.Logging.DriverLoggerUtil;
using static Neo4j.Driver.Internal.Util.ConfigBuilders;

namespace Neo4j.Driver.Internal
{
    internal partial class AsyncSession : AsyncQueryRunner, IInternalAsyncSession
    {
        // If the connection is ever successfully created, 
        // then it is session's responsibility to dispose them properly
        // without any possible connection leak.
        private readonly IConnectionProvider _connectionProvider;

        private readonly AccessMode _defaultMode;
        private IConnection _connection;
        private Task<IResultCursor> _result; // last session run result if any

        private AsyncTransaction _transaction;

        private readonly IAsyncRetryLogic _retryLogic;
        private bool _isOpen = true;
        private bool _disposed = false;

        private Bookmarks _bookmarks;

        private readonly ILogger _logger;

        [Obsolete("Replaced by more sensibly named LastBookmarks. Will be removed in 6.0")]
        public Bookmark LastBookmark => _bookmarks;

        public Bookmarks LastBookmarks => _bookmarks;

        private string _database;
        private readonly bool _reactive;
        private readonly long _fetchSize;

        public AsyncSession(IConnectionProvider provider, ILogger logger, IAsyncRetryLogic retryLogic = null,
            AccessMode defaultMode = AccessMode.Write,
            string database = null,
            Bookmarks bookmarks = null, bool reactive = false, long fetchSize = Config.Infinite)
        {
            _connectionProvider = provider;
            _logger = logger;
            _retryLogic = retryLogic;
            _reactive = reactive;

            _database = database;
            _defaultMode = defaultMode;
            _fetchSize = fetchSize;
            UpdateBookmarks(bookmarks);
        }

        public Task<IResultCursor> RunAsync(Query query, Action<TransactionConfigBuilder> action)
        {
            return RunAsync(query, action, true);
        }

        public SessionConfig SessionConfig { internal set; get; }

        public Task<IResultCursor> RunAsync(string query, Action<TransactionConfigBuilder> action)
        {
            return RunAsync(new Query(query), action);
        }

        public Task<IResultCursor> RunAsync(string query, IDictionary<string, object> parameters,
            Action<TransactionConfigBuilder> action)
        {
            return RunAsync(new Query(query, parameters), action);
        }

        public override Task<IResultCursor> RunAsync(Query query)
        {
            return RunAsync(query, null);
        }

        public Task<IAsyncTransaction> BeginTransactionAsync()
        {
            return BeginTransactionAsync(null);
        }

        public Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionConfigBuilder> action)
        {
            return BeginTransactionAsync(action, true);
        }

        public async Task<IAsyncTransaction> BeginTransactionAsync(Action<TransactionConfigBuilder> action,
            bool disposeUnconsumedSessionResult)
        {
            var tx = await TryExecuteAsync(_logger,
                    () => BeginTransactionWithoutLoggingAsync(_defaultMode, action, disposeUnconsumedSessionResult))
                .ConfigureAwait(false);
            return tx;
        }

        public async Task<IAsyncTransaction> BeginTransactionAsync(AccessMode mode,
            Action<TransactionConfigBuilder> action, bool disposeUnconsumedSessionResult)
        {
            var tx = await TryExecuteAsync(_logger,
                    () => BeginTransactionWithoutLoggingAsync(mode, action, disposeUnconsumedSessionResult))
                .ConfigureAwait(false);
            return tx;
        }

        public Task<IResultCursor> RunAsync(Query query, Action<TransactionConfigBuilder> action,
            bool disposeUnconsumedSessionResult)
        {
            var options = BuildTransactionConfig(action);
            var result = TryExecuteAsync(_logger, async () =>
            {
                await EnsureCanRunMoreQuerysAsync(disposeUnconsumedSessionResult).ConfigureAwait(false);

                await AcquireConnectionAndDbNameAsync(_defaultMode).ConfigureAwait(false);

                var protocol = _connection.BoltProtocol;

                return await protocol
                    .RunInAutoCommitTransactionAsync(_connection, query, _reactive, this, this, _database,
                        _bookmarks, options, ImpersonatedUser(), _fetchSize)
                    .ConfigureAwait(false);
            });

            _result = result;
            return result;
        }

        public Task<T> ReadTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work,
            Action<TransactionConfigBuilder> action = null)
        {
            return RunTransactionAsync(AccessMode.Read, work, action);
        }

        public Task ReadTransactionAsync(Func<IAsyncTransaction, Task> work,
            Action<TransactionConfigBuilder> action = null)
        {
            return RunTransactionAsync(AccessMode.Read, work, action);
        }

        public Task<T> WriteTransactionAsync<T>(Func<IAsyncTransaction, Task<T>> work,
            Action<TransactionConfigBuilder> action = null)
        {
            return RunTransactionAsync(AccessMode.Write, work, action);
        }

        public Task WriteTransactionAsync(Func<IAsyncTransaction, Task> work,
            Action<TransactionConfigBuilder> action = null)
        {
            return RunTransactionAsync(AccessMode.Write, work, action);
        }

        private Task RunTransactionAsync(AccessMode mode, Func<IAsyncTransaction, Task> work,
            Action<TransactionConfigBuilder> action)
        {
            return RunTransactionAsync(mode, async tx =>
            {
                await work(tx).ConfigureAwait(false);
                var ignored = 1;
                return ignored;
            }, action);
        }

        private Task<T> RunTransactionAsync<T>(AccessMode mode, Func<IAsyncTransaction, Task<T>> work,
            Action<TransactionConfigBuilder> action)
        {
            return TryExecuteAsync(_logger, () => _retryLogic.RetryAsync(async () =>
            {
                var tx = await BeginTransactionWithoutLoggingAsync(mode, action, true).ConfigureAwait(false);
                try
                {
                    var result = await work(tx).ConfigureAwait(false);
                    if (tx.IsOpen)
                    {
                        await tx.CommitAsync().ConfigureAwait(false);
                    }

                    return result;
                }
                catch
                {
                    if (tx.IsOpen)
                    {
                        await tx.RollbackAsync().ConfigureAwait(false);
                    }

                    throw;
                }
            }));
        }

        private async Task<IInternalAsyncTransaction> BeginTransactionWithoutLoggingAsync(AccessMode mode,
            Action<TransactionConfigBuilder> action, bool disposeUnconsumedSessionResult)
        {
            var config = BuildTransactionConfig(action);
            await EnsureCanRunMoreQuerysAsync(disposeUnconsumedSessionResult).ConfigureAwait(false);

            await AcquireConnectionAndDbNameAsync(mode).ConfigureAwait(false);

            var tx = new AsyncTransaction(_connection, this, _logger, _database, _bookmarks, _reactive, _fetchSize,
                ImpersonatedUser());
            await tx.BeginTransactionAsync(config).ConfigureAwait(false);
            _transaction = tx;
            return _transaction;
        }

        private async Task AcquireConnectionAndDbNameAsync(AccessMode mode)
        {
            _connection = await _connectionProvider.AcquireAsync(mode, _database, ImpersonatedUser(), _bookmarks)
                .ConfigureAwait(false);

            //Update the database. If a routing request occurred it may have returned a differing DB alias name that needs to be used for the 
            //rest of the sessions lifetime.
            _database = _connection.Database;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                //Dispose managed resources

                //call it synchronously
                CloseAsync().GetAwaiter().GetResult();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            await CloseAsync().ConfigureAwait(false);
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }

        private string ImpersonatedUser()
        {
            return SessionConfig is not null ? SessionConfig.ImpersonatedUser : string.Empty;
        }

        public async Task ExecuteAsync(Func<IEagerQueryRunner, CancellationToken, Task> work, TxAccess txAccess,
            SessionTxConfig config = null,
            CancellationToken cancellationToken = default)
        {
            config ??= new SessionTxConfig();
            await EnsureCanRunMoreQuerysAsync(true).ConfigureAwait(false);
            var accessMode = txAccess == TxAccess.Readers ? AccessMode.Read : AccessMode.Write;
            var tx = await BeginTransactionWithoutLoggingAsync(accessMode, 
                    cb => cb.WithMetadata(config.Metadata).WithTimeout(config.Timeout), true)
                .ConfigureAwait(false);
            try
            {
                await work(tx, cancellationToken).ConfigureAwait(false);
                if (tx.IsOpen)
                {
                    await tx.CommitAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                if (tx.IsOpen)
                {
                    await tx.RollbackAsync().ConfigureAwait(false);
                }

                throw;
            }
        }

        public async Task<TResult> ExecuteAsync<TResult>(Func<IEagerQueryRunner, CancellationToken, Task<TResult>> work,
            TxAccess txAccess,
            SessionTxConfig config = null, CancellationToken cancellationToken = default)
        {
            config ??= new SessionTxConfig();
            await EnsureCanRunMoreQuerysAsync(true).ConfigureAwait(false);
            var accessMode = txAccess == TxAccess.Readers ? AccessMode.Read : AccessMode.Write;
            var tx = await BeginTransactionWithoutLoggingAsync(accessMode, 
                    cb => cb.WithMetadata(config.Metadata).WithTimeout(config.Timeout), true)
                .ConfigureAwait(false);
            try
            {
                var result = await work(tx, cancellationToken).ConfigureAwait(false);
                if (tx.IsOpen)
                {
                    await tx.CommitAsync().ConfigureAwait(false);
                }

                return result;
            }
            catch
            {
                if (tx.IsOpen)
                {
                    await tx.RollbackAsync().ConfigureAwait(false);
                }

                throw;
            }
        }

        public Task<IRecordSetResult> QueryAsync(Query query, SessionQueryConfig queryConfig = null,
            CancellationToken cancellationToken = default)
        {
            queryConfig ??= new SessionQueryConfig();
            return new UdfRetryLogic(queryConfig.RetryFunc, queryConfig.MaxRetry)
                .RetryAsync(() =>
                    queryConfig.ExecuteInTransaction 
                        ? ExecuteAsync(
                            (tx, ct) => tx.QueryAsync(query, queryConfig, ct),
                            queryConfig.Access == Access.Readers ? TxAccess.Readers : TxAccess.Writers,
                            null,
                            cancellationToken)
                        : RunQueryAsync(query, queryConfig, cancellationToken));
        }

        private async Task<IRecordSetResult> RunQueryAsync(Query query, SessionQueryConfig queryConfig,
            CancellationToken cancellationToken)
        {
            await EnsureCanRunMoreQuerysAsync(true).ConfigureAwait(false);
            var accessMode = queryConfig.Access == Access.Readers ? AccessMode.Read : AccessMode.Write;
            var session = new AsyncSession(_connectionProvider, _logger, null, accessMode, _database,
                LastBookmarks);
            await using (session.ConfigureAwait(false))
            {
                var cursor = await session.RunAsync(query).ConfigureAwait(false);
                return await cursor.ToResultAsync(queryConfig, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public Task<IRecordSetResult> QueryAsync(string query, object parameters = null,
            Access access = Access.Automatic,
            CancellationToken cancellationToken = default) => QueryAsync(new Query(query, parameters),
            new SessionQueryConfig {Access = access}, cancellationToken);

        public Task<IRecordSetResult> QueryAsync(string query, Dictionary<string, object> parameters,
            Access access = Access.Automatic,
            CancellationToken cancellationToken = default) => QueryAsync(new Query(query, parameters),
            new SessionQueryConfig {Access = access}, cancellationToken);

        public Task<IRecordSetResult> QueryAsync(string query, object parameters, SessionQueryConfig config,
            CancellationToken cancellationToken = default) =>
            QueryAsync(new Query(query, parameters), config, cancellationToken);

        public Task<IRecordSetResult> QueryAsync(string query, Dictionary<string, object> parameters,
            SessionQueryConfig config,
            CancellationToken cancellationToken = default) =>
            QueryAsync(new Query(query, parameters), config, cancellationToken);

        public Task<IRecordSetResult> QueryAsync(Query query, Access access = Access.Automatic,
            CancellationToken cancellationToken = default) =>
            QueryAsync(query, new SessionQueryConfig {Access = access}, cancellationToken);
    }
}