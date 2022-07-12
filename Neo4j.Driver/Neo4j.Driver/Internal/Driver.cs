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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Metrics;
using Neo4j.Driver.Internal.Util;
using Neo4j.Driver.Internal.Routing;

namespace Neo4j.Driver.Internal
{
    internal class Driver : IInternalDriver
    {
        private int _closedMarker;

        private readonly IConnectionProvider _connectionProvider;
        private readonly IAsyncRetryLogic _retryLogic;
        private readonly ILogger _logger;
        private readonly IMetrics _metrics;
        private readonly Config _config;

        public Uri Uri { get; }
        public bool Encrypted { get; }

        internal Driver(Uri uri,
            bool encrypted,
            IConnectionProvider connectionProvider,
            IAsyncRetryLogic retryLogic,
            ILogger logger = null,
            IMetrics metrics = null,
            Config config = null)
        {
            Throw.ArgumentNullException.IfNull(connectionProvider, nameof(connectionProvider));

            Uri = uri;
            Encrypted = encrypted;
            _logger = logger;
            _connectionProvider = connectionProvider;
            _retryLogic = retryLogic;
            _metrics = metrics;
            _config = config;
        }

        private bool IsClosed => _closedMarker > 0;

        public Config Config => _config;

        public IAsyncSession AsyncSession()
        {
            return AsyncSession(null);
        }

        public IAsyncSession AsyncSession(Action<SessionConfigBuilder> action)
        {
            return Session(action, false);
        }

        public IInternalAsyncSession Session(Action<SessionConfigBuilder> action, bool reactive)
        {
            if (IsClosed)
            {
                ThrowDriverClosedException();
            }

            var sessionConfig = ConfigBuilders.BuildSessionConfig(action);

            var session = new AsyncSession(_connectionProvider, 
                                           _logger, 
                                           _retryLogic, 
                                           sessionConfig.DefaultAccessMode,
                                           sessionConfig.Database, 
                                           Bookmarks.From(sessionConfig.Bookmarks ?? Array.Empty<Bookmarks>()), 
                                           reactive, ParseFetchSize(sessionConfig.FetchSize)) {SessionConfig = sessionConfig};

            if (IsClosed)
            {
                ThrowDriverClosedException();
            }

            return session;
        }

        private long ParseFetchSize(long? fetchSize)
        {
            return fetchSize.GetValueOrDefault(_config.FetchSize);
        }

        private void Close()
        {
            CloseAsync().GetAwaiter().GetResult();
        }

        public Task CloseAsync()
        {
            return Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0 
                ? _connectionProvider.CloseAsync() 
                : Task.CompletedTask;
        }

        public Task<IServerInfo> GetServerInfoAsync() =>
            _connectionProvider.VerifyConnectivityAndGetInfoAsync();

        public Task VerifyConnectivityAsync() => GetServerInfoAsync();

        public Task<bool> SupportsMultiDbAsync()
        {
            return _connectionProvider.SupportsMultiDbAsync();
        }

        //Non public facing api. Used for testing with testkit only
        public IRoutingTable GetRoutingTable(string database)
        {
            return _connectionProvider.GetRoutingTable(database);		
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (IsClosed)
                return;
            await CloseAsync().ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsClosed)
                return;

            if (disposing)
            {
                Close();
            }
        }

        private void ThrowDriverClosedException()
        {
            throw new ObjectDisposedException(GetType().Name,
                "Cannot open a new session on a driver that is already disposed.");
        }

        internal IMetrics GetMetrics()
        {
            if (_metrics == null)
            {
                throw new InvalidOperationException(
                    "Cannot access driver metrics if it is not enabled when creating this driver.");
            }

            return _metrics;
        }

        public Task<IRecordSetResult> QueryAsync(string query, object parameters = null,
            ClusterMemberAccess clusterMemberAccess = ClusterMemberAccess.Automatic,
            CancellationToken cancellationToken = default)
        {
            return QueryAsync(new Query(query, parameters), new DriverQueryConfig {ClusterMemberAccess = clusterMemberAccess},
                cancellationToken);
        }

        public Task<IRecordSetResult> QueryAsync(string query, Dictionary<string, object> parameters,
            ClusterMemberAccess clusterMemberAccess = ClusterMemberAccess.Automatic,
            CancellationToken cancellationToken = default)
        {
            return QueryAsync(new Query(query, parameters), new DriverQueryConfig { ClusterMemberAccess = clusterMemberAccess },
                cancellationToken);
        }

        public Task<IRecordSetResult> QueryAsync(string query, object parameters, DriverQueryConfig queryConfig,
            CancellationToken cancellationToken = default)
        {
            return QueryAsync(new Query(query, parameters), queryConfig, cancellationToken);
        }

        public Task<IRecordSetResult> QueryAsync(string query, Dictionary<string, object> parameters, DriverQueryConfig queryConfig,
            CancellationToken cancellationToken = default)
        {
            return QueryAsync(new Query(query, parameters), queryConfig, cancellationToken);
        }

        public Task<IRecordSetResult> QueryAsync(Query query, DriverQueryConfig queryConfig, CancellationToken cancellationToken = default)
        {
            // assert config is valid
            // - if automatic, ensure can complete query (bolt / SSR / Guesstimator)
            // - 
            // create session
            // create retry
            // create transaction
            // execute query
            // create IRecordResultSet
            //    populate records & summary.
            // update bookmarks
            // return result.
            return null;
        }

        public Task ExecuteAsync(Func<IAsyncQueryRunner>, Task> work, TransactionClusterMemberAccess clusterMemberAccess, TxConfig config = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> ExecuteAsync<T>(Func<IConfigurableQueryRunner<>, Task<T>> work, TransactionClusterMemberAccess clusterMemberAccess, TxConfig config = null)
        {
            throw new NotImplementedException();
        }
    }
}