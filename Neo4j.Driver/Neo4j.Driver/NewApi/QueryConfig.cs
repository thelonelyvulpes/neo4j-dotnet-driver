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

public record DriverQueryConfig : SessionQueryConfig
{
    public static readonly DriverQueryConfig Read = new DriverQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Readers
    };

    public static readonly DriverQueryConfig Write = new DriverQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Writers
    };

    public static readonly DriverQueryConfig AutoCommit = new DriverQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Writers,
        MaxRetry = 0
    };

    public Bookmarks Bookmarks { get; init; } = null;

    public string DbName { get; init; } = null;
}

public record SessionQueryConfig
{

    public static readonly SessionQueryConfig Read = new SessionQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Readers
    };

    public static readonly SessionQueryConfig Write = new SessionQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Writers
    };

    public static readonly SessionQueryConfig AutoCommit = new SessionQueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Writers,
        MaxRetry = 0
    };

    public ClusterMemberAccess ClusterMemberAccess { get; init; } = ClusterMemberAccess.Automatic;

    public Dictionary<string, string> Metadata { get; init; } = null;

    public TimeSpan Timeout { get; init; }

    public int MaxRetry { get; init; }
}
