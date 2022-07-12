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
public class DriverQueryConfig : SessionQueryConfig
{
    /// <summary>
    /// 
    /// </summary>
    public static readonly DriverQueryConfig Read = new DriverQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Readers
    };

    /// <summary>
    /// 
    /// </summary>
    public static readonly DriverQueryConfig Write = new DriverQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Writers
    };

    /// <summary>
    /// 
    /// </summary>w
    public static readonly DriverQueryConfig AutoCommit = new DriverQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Writers,
        MaxRetry = 0
    };

    /// <summary>
    /// 
    /// </summary>
    public Bookmarks Bookmarks { get; set; } = null;
    /// <summary>
    /// 
    /// </summary>
    public string DbName { get; set; } = null;
}

public class SessionQueryConfig
{
    /// <summary>
    /// 
    /// </summary>
    public static readonly SessionQueryConfig Read = new SessionQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Readers
    };

    /// <summary>
    /// 
    /// </summary>
    public static readonly SessionQueryConfig Write = new SessionQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Writers
    };

    /// <summary>
    /// 
    /// </summary>w
    public static readonly SessionQueryConfig AutoCommit = new SessionQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Writers,
        MaxRetry = 0
    };

    /// <summary>
    /// 
    /// </summary>
    public ClusterMemberAccess ClusterMemberAccess { get; set; } = ClusterMemberAccess.Automatic;

    /// <summary>
    /// 
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = null;

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public int MaxRetry { get; set; }
}
