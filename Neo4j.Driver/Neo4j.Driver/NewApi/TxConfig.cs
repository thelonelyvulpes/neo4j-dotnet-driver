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

public class SessionTxConfig
{
    /// <summary>
    /// 
    /// </summary>
    public int MaxRetry { get; set; } = 2;
    /// <summary>
    /// 
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public TimeSpan Timeout { get; set; }
}


public class DriverTransactionConfig : SessionTxConfig
{
    /// <summary>
    /// 
    /// </summary>
    public string DbName { get; set; } = null;
    /// <summary>
    /// 
    /// </summary>
    public Bookmarks Bookmarks { get; set; } = null;
}