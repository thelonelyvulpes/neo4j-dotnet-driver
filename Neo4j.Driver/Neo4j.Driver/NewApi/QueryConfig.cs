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
public record DriverQueryConfig : SessionQueryConfig
{
    /// <summary>
    /// 
    /// </summary>
    public new static readonly DriverQueryConfig Read = new()
    {
        Access = Access.Readers,
    };

    /// <summary>
    /// 
    /// </summary>
    public new static readonly DriverQueryConfig Write = new()
    {
        Access = Access.Writers
    };

    /// <summary>
    /// 
    /// </summary>
    public new static readonly DriverQueryConfig AutoCommit = new()
    {
        Access = Access.Writers,
        ExecuteInTransaction = false,
        MaxRetry = 0
    };

    /// <summary>
    /// 
    /// </summary>
    public static readonly DriverQueryConfig Automatic = new()
    {
        Access = Access.Automatic,
        MaxRetry = 0
    };
    /// <summary>
    /// 
    /// </summary>
    public Bookmarks Bookmarks { get; init; } = null;
    /// <summary>
    /// 
    /// </summary>
    public string Database { get; init; } = null;

    /// <summary>
    /// 
    /// </summary>
    public string ImpersonatedUser { get; init; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sessionConfigBuilder"></param>
    /// <param name="bookmarks"></param>
    internal void ConfigureSession(SessionConfigBuilder sessionConfigBuilder, Bookmarks bookmarks)
    {
        sessionConfigBuilder.WithBookmarks(Bookmarks ?? bookmarks);

        if (Database != null)
            sessionConfigBuilder.WithDatabase(Database);

        if (ImpersonatedUser != null)
            sessionConfigBuilder.WithImpersonatedUser(ImpersonatedUser);
    }
}

/// <summary>
/// 
/// </summary>
public record SessionQueryConfig : QueryConfig
{
    /// <summary>
    /// 
    /// </summary>
    public static readonly SessionQueryConfig Read = new()
    {
        Access = Access.Readers
    };

    /// <summary>
    /// 
    /// </summary>
    public static readonly SessionQueryConfig Write = new()
    {
        Access = Access.Writers
    };

    /// <summary>
    /// 
    /// </summary>
    public static readonly SessionQueryConfig AutoCommit = new()
    {
        Access = Access.Writers,
        ExecuteInTransaction = false,
        RetryFunc = Retries.NoRetry,
        MaxRetry = 0
    };

    /// <summary>
    /// 
    /// </summary>
    public Access Access { get; init; } = Access.Automatic;

    /// <summary>
    /// 
    /// </summary>
    public bool ExecuteInTransaction { get; init; } = true;

    /// <summary>
    /// 
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = null;

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan Timeout { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public int MaxRetry { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public Func<Exception, int, int, (bool, TimeSpan)> RetryFunc { get; set; }
}

/// <summary>
/// 
/// </summary>
public record QueryConfig
{
    /// <summary>
    /// 
    /// </summary>
    public bool SkipRecords { get; init; } = false;
    /// <summary>
    /// 
    /// </summary>
    public int MaxRecords { get; init; } = 1000;

    public static readonly QueryConfig Default = new QueryConfig();
}
