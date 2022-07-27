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
using System.Collections.Generic;

namespace Neo4j.Driver;

/// <summary>
/// 
/// </summary>
public record SessionTxConfig
{
    /// <summary>
    /// 
    /// </summary>
    public int MaxRetry { get; init; } = 2;


    public Func<Exception, int, int, (bool, TimeSpan)> RetryFunc { get; init; } = Retries.Transient;

    /// <summary>
    /// 
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// 
    /// </summary>
    public TransactionConfig TransactionConfig => new TransactionConfig
    {
        Metadata = Metadata,
        Timeout = Timeout
    };
}


/// <summary>
/// 
/// </summary>
public record DriverTxConfig : SessionTxConfig
{
    /// <summary>
    /// 
    /// </summary>
    public string Database { get; init; } = null;
    /// <summary>
    /// 
    /// </summary>
    public Bookmarks Bookmarks { get; init; } = null;
    /// <summary>
    /// 
    /// </summary>
    public string ImpersonatedUser { get; init; } = null;

    public void ConfigureSession(SessionConfigBuilder sessionConfigBuilder, Bookmarks bookmarks)
    {
        sessionConfigBuilder.WithBookmarks(Bookmarks ?? bookmarks);

        if (Database != null)
            sessionConfigBuilder.WithDatabase(Database);

        if (ImpersonatedUser != null)
            sessionConfigBuilder.WithImpersonatedUser(ImpersonatedUser);
    }
}