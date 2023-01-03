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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Routing;

internal class RoutingTableManager : IRoutingTableManager
{
    private readonly IDiscovery _discovery;
    private readonly IInitialServerAddressProvider _initialServerAddressProvider;
    private readonly ILogger _logger;

    private readonly IClusterConnectionPoolManager _poolManager;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _routingTableLocks = new();

    private readonly TimeSpan _routingTablePurgeDelay;

    private readonly ConcurrentDictionary<string, IRoutingTable> _routingTables = new();

    public RoutingTableManager(
        RoutingSettings routingSettings,
        IClusterConnectionPoolManager poolManager,
        ILogger logger) :
        this(
            routingSettings.InitialServerAddressProvider,
            new ClusterDiscovery(),
            poolManager,
            logger,
            routingSettings.RoutingTablePurgeDelay)
    {
    }

    public RoutingTableManager(
        IInitialServerAddressProvider initialServerAddressProvider,
        IDiscovery discovery,
        IClusterConnectionPoolManager poolManager,
        ILogger logger,
        TimeSpan routingTablePurgeDelay,
        params IRoutingTable[] routingTables)
    {
        _initialServerAddressProvider = initialServerAddressProvider;
        _discovery = discovery;
        _poolManager = poolManager;
        _logger = logger;
        _routingTablePurgeDelay = routingTablePurgeDelay;

        foreach (var routingTable in routingTables)
        {
            _routingTables.TryAdd(routingTable.Database, routingTable);
        }
    }

    public async Task<IRoutingTable> EnsureRoutingTableForModeAsync(
        AccessMode mode,
        string database,
        string impersonatedUser,
        Bookmarks bookmarks)
    {
        database = database ?? string.Empty;

        var semaphore = GetLock(database);

        // now lock
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_routingTables.TryGetValue(database, out var existingTable) &&
                !existingTable.IsStale(mode))
            {
                return existingTable;
            }

            var refreshedTable = await UpdateRoutingTableAsync(mode, database, impersonatedUser, bookmarks)
                .ConfigureAwait(false);

            await UpdateAsync(refreshedTable).ConfigureAwait(false);
            return refreshedTable;
        }
        finally
        {
            // no matter whether we succeeded to update or not, we release the lock
            semaphore.Release();
        }
    }

    public async Task<IServerInfo> GetServerInfoAsync(Uri uri, string database)
    {
        var bufferedExceptions = new List<Exception>();
        var conn = await _poolManager.CreateClusterConnectionAsync(uri).ConfigureAwait(false);
        if (conn == null)
        {
            throw new ServiceUnavailableException("Could not create connection");
        }

        var rt = await _discovery.DiscoverAsync(conn, null, null, null)
            .ConfigureAwait(false);

        await conn.CloseAsync().ConfigureAwait(false);
        await UpdateAsync(rt).ConfigureAwait(false);
        foreach (var table in rt.Readers)
        {
            try
            {
                var reportedConnection =
                    await _poolManager.CreateClusterConnectionAsync(table).ConfigureAwait(false);

                await reportedConnection.CloseAsync().ConfigureAwait(false);
                return reportedConnection.Server;
            }
            catch (Exception ex) when (ex is not AuthenticationException)
            {
                bufferedExceptions.Add(ex);
            }
        }

        throw new ServiceUnavailableException(
            $"Failed to find server info for '{uri}' for database '{database}'.",
            new AggregateException(bufferedExceptions));
    }

    public void Clear()
    {
        _routingTables.Clear();
        _routingTableLocks.Clear();
    }

    public void ForgetServer(Uri uri, string database)
    {
        var routingTable = RoutingTableFor(database);
        routingTable?.Remove(uri);
    }

    public void ForgetWriter(Uri uri, string database)
    {
        var routingTable = RoutingTableFor(database);
        routingTable?.RemoveWriter(uri);
    }

    public IRoutingTable RoutingTableFor(string database)
    {
        return _routingTables.TryGetValue(database ?? string.Empty, out var routingTable) ? routingTable : null;
    }

    private SemaphoreSlim GetLock(string database)
    {
        return _routingTableLocks.GetOrAdd(database, _ => new SemaphoreSlim(1, 1));
    }

    private async Task UpdateAsync(IRoutingTable newRoutingTable)
    {
        IRoutingTable UpdateRoutingTable(
            IRoutingTable oldTable,
            IRoutingTable newTable,
            out IEnumerable<Uri> added,
            out IEnumerable<Uri> removed)
        {
            var allNew = newTable.All().ToArray();
            var allKnown = oldTable == null ? Array.Empty<Uri>() : oldTable.All().ToArray();
            added = allNew.Except(allKnown);
            removed = allKnown.Except(allNew);
            return newTable;
        }

        IEnumerable<Uri> addedServers = null, removedServers = null;
        _routingTables.AddOrUpdate(
            newRoutingTable.Database,
            _ => UpdateRoutingTable(null, newRoutingTable, out addedServers, out removedServers),
            (_, oldTable) => UpdateRoutingTable(oldTable, newRoutingTable, out addedServers, out removedServers));

        await _poolManager.UpdateConnectionPoolAsync(addedServers, removedServers).ConfigureAwait(false);

        PurgeAged();

        _logger?.Info("Routing table is updated => {0}", newRoutingTable);
    }

    private void PurgeAged()
    {
        foreach (var routingTable in _routingTables.Values)
        {
            if (!routingTable.IsExpiredFor(_routingTablePurgeDelay))
            {
                continue;
            }

            _routingTables.TryRemove(routingTable.Database, out _);
            _routingTableLocks.TryRemove(routingTable.Database, out _);
        }
    }

    private Task PrependRoutersAsync(IRoutingTable routingTable, ISet<Uri> uris)
    {
        routingTable.PrependRouters(uris);
        return _poolManager.AddConnectionPoolAsync(uris);
    }

    internal async Task<IRoutingTable> UpdateRoutingTableAsync(
        AccessMode mode,
        string database,
        string impersonatedUser,
        Bookmarks bookmarks)
    {
        if (database == null)
        {
            throw new ArgumentNullException(nameof(database));
        }

        _logger?.Debug("Updating routing table for database '{0}'.", database);

        var existingTable = RoutingTableFor(database);
        if (existingTable == null)
        {
            existingTable = new RoutingTable(database, Enumerable.Empty<Uri>());
        }

        var hasPrependedInitialRouters = false;
        if (existingTable.IsReadingInAbsenceOfWriter(mode))
        {
            var uris = _initialServerAddressProvider.Get();
            await PrependRoutersAsync(existingTable, uris).ConfigureAwait(false);
            hasPrependedInitialRouters = true;
        }

        var triedUris = new HashSet<Uri>();
        var newRoutingTable = await UpdateRoutingTableAsync(
                existingTable,
                mode,
                database,
                impersonatedUser,
                bookmarks,
                triedUris)
            .ConfigureAwait(false);

        if (newRoutingTable != null)
        {
            return newRoutingTable;
        }

        if (!hasPrependedInitialRouters)
        {
            var uris = _initialServerAddressProvider.Get();
            uris.ExceptWith(triedUris);
            if (uris.Count != 0)
            {
                await PrependRoutersAsync(existingTable, uris).ConfigureAwait(false);
                newRoutingTable = await UpdateRoutingTableAsync(
                        existingTable,
                        mode,
                        database,
                        impersonatedUser,
                        bookmarks)
                    .ConfigureAwait(false);

                if (newRoutingTable != null)
                {
                    return newRoutingTable;
                }
            }
        }

        // We tried our best however there is just no cluster.
        // This is the ultimate place we will inform the user that a new driver to be created.
        throw new ServiceUnavailableException(
            "Failed to connect to any routing server. " +
            "Please make sure that the cluster is up and can be accessed by the driver and retry.");
    }

    internal async Task<IRoutingTable> UpdateRoutingTableAsync(
        IRoutingTable routingTable,
        AccessMode mode,
        string database,
        string impersonatedUser,
        Bookmarks bookmarks,
        ISet<Uri> triedUris = null)
    {
        if (database == null)
        {
            throw new ArgumentNullException(nameof(database));
        }

        var knownRouters = routingTable?.Routers ?? throw new ArgumentNullException(nameof(routingTable));
        foreach (var router in knownRouters)
        {
            triedUris?.Add(router);
            try
            {
                var conn = await _poolManager.CreateClusterConnectionAsync(router).ConfigureAwait(false);
                
                if (conn == null)
                {
                    routingTable.Remove(router);
                }
                else
                {
                    try
                    {
                        var newRoutingTable =
                            await _discovery.DiscoverAsync(conn, database, impersonatedUser, bookmarks)
                                .ConfigureAwait(false);

                        if (!newRoutingTable.IsStale(mode))
                        {
                            return newRoutingTable;
                        }

                        _logger?.Debug(
                            "Skipping stale routing table received from server '{0}' for database '{1}'",
                            router,
                            database);
                    }
                    finally
                    {
                        await conn.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                var failfast = IsFailFastException(ex);
                var logMsg = "Failed to update routing table from server '{0}' for database '{1}'.";
                if (ex is Neo4jException ne)
                {
                    logMsg += " Error code: '{2}'";
                    var code = ne.Code;
                    if (failfast)
                    {
                        _logger?.Error(ex, logMsg, router, database, code);
                    }
                    else
                    {
                        _logger?.Warn(ex, logMsg, router, database, code);
                    }
                }
                else
                {
                    _logger?.Warn(ex, logMsg, router, database);
                }

                if (failfast)
                {
                    throw;
                }
            }
        }

        return null;
    }

    private static bool IsFailFastException(Exception ex)
    {
        return ex
            is FatalDiscoveryException // Neo.ClientError.Database.DatabaseNotFound
            or InvalidBookmarkException // Neo.ClientError.Transaction.InvalidBookmark
            or InvalidBookmarkMixtureException // Neo.ClientError.Transaction.InvalidBookmarkMixture
            or ArgumentErrorException // Neo.ClientError.Statement.ArgumentError
            or ProtocolException // Neo.ClientError.Request.Invalid and (special to .NET driver) Neo.ClientError.Request.InvalidFormat
            or TypeException // Neo.ClientError.Statement.TypeError
            or SecurityException // Neo.ClientError.Security.*
            and not AuthorizationException; // except Neo.ClientError.Security.AuthorizationExpired
    }
}
