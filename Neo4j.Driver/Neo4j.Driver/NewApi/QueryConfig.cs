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
public class QueryConfig
{
    /// <summary>
    /// 
    /// </summary>
    public static readonly QueryConfig Read = new QueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Readers
    };

    /// <summary>
    /// 
    /// </summary>
    public static readonly QueryConfig Write = new QueryConfig
    {
        ClusterMemberAccess = ClusterMemberAccess.Writers
    };

    /// <summary>
    /// 
    /// </summary>
    public static readonly QueryConfig CypherTransaction = new QueryConfig
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
    public int MaxRetry { get; set; } = 2;
    /// <summary>
    /// 
    /// </summary>
    public Func<Exception, int, int, (bool retry, TimeSpan delay)> RetryFunc { get; set; } = Retries.Transient;
}
