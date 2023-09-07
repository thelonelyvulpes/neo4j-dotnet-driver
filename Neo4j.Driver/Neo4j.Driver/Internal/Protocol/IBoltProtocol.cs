// Copyright (c) "Neo4j"
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;

namespace Neo4j.Driver.Internal;

internal interface IBoltProtocol
{
    ValueTask AuthenticateAsync(
        IConnection connection,
        string userAgent,
        IAuthToken authToken,
        INotificationsConfig notificationsConfig);

    ValueTask LogoutAsync(IConnection connection);
    ValueTask ResetAsync(IConnection connection);
    ValueTask ReAuthAsync(IConnection connection, IAuthToken newAuthToken);

    ValueTask<IReadOnlyDictionary<string, object>> GetRoutingTableAsync(
        IConnection connection,
        string database,
        SessionConfig sessionConfig,
        Bookmarks bookmarks);

    ValueTask<IResultCursor> RunInAutoCommitTransactionAsync(
        IConnection connection,
        AutoCommitParams autoCommitParams,
        INotificationsConfig notificationsConfig);

    ValueTask BeginTransactionAsync(IConnection connection, BeginProtocolParams beginParams);

    ValueTask<IResultCursor> RunInExplicitTransactionAsync(
        IConnection connection,
        Query query,
        bool reactive,
        long fetchSize = Config.Infinite);

    ValueTask CommitTransactionAsync(IConnection connection, IBookmarksTracker bookmarksTracker);
    ValueTask RollbackTransactionAsync(IConnection connection);
}
