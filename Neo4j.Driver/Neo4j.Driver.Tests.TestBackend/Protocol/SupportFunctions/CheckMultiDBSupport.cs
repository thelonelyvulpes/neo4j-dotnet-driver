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

using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class CheckMultiDBSupport : IProtocolObject
{
    public CheckMultiDBSupportType data { get; set; } = new();

    [JsonIgnore] private bool MutlitDBSupportAvailable { get; set; }

    public override async Task Process()
    {
        var driver = ((NewDriver)ObjManager.GetObject(data.driverId)).Driver;
        MutlitDBSupportAvailable = await driver.SupportsMultiDbAsync();
    }

    public override string Respond()
    {
        return new ProtocolResponse("MultiDBSupport", new { id = uniqueId, available = MutlitDBSupportAvailable })
            .Encode();
    }

    public class CheckMultiDBSupportType
    {
        public string driverId { get; set; }
    }
}
