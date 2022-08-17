﻿// Copyright (c) 2002-2022 "Neo4j,"
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

using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Routing;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class GetRoutingTable : ProtocolObject
{
    public GetRoutingTableDataType data { get; set; } = new();

    [JsonIgnore] public IRoutingTable RoutingTable { get; set; }

    public override async Task ProcessAsync(Controller controller)
    {
        var protocolDriver = (NewDriver) ObjManager.GetObject(data.driverId);
        var driver = (Internal.Driver) protocolDriver.Driver;
        RoutingTable = driver.GetRoutingTable(data.database);

        await Task.CompletedTask;
    }

    public override string Respond()
    {
        return new ProtocolResponse("RoutingTable", new
        {
            database = RoutingTable.Database,
            ttl = "huh",
            routers = RoutingTable.Routers.Select(x => x.Authority),
            readers = RoutingTable.Readers.Select(x => x.Authority),
            writers = RoutingTable.Writers.Select(x => x.Authority)
        }).Encode();
    }

    public class GetRoutingTableDataType
    {
        public string driverId { get; set; }
        public string database { get; set; }
    }
}