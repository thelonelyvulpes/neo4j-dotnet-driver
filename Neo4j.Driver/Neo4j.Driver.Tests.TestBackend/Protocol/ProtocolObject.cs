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

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal abstract class ProtocolObject
{
    public string name { get; set; }

    [JsonProperty("id")] 
    public string IdHolder => UniqueId;

    //Only exposes the get option so that the serializer will output it.
    //Don't want to read in on deserialization.
    [JsonIgnore]
    public string UniqueId { get; internal set; }

    [JsonIgnore] 
    protected ProtocolObjectManager ObjManager { get; set; }

    public Action ProtocolEvent;

    public void SetObjectManager(ProtocolObjectManager objManager)
    {
        ObjManager = objManager;
    }

    //Default is to not use the controller object.
    //But option to override this method and use it if necessary.
    public virtual Task ProcessAsync(Controller controller) => ProcessAsync();
    public virtual Task ProcessAsync() => Task.CompletedTask;

    //Default is to not use the controller object.
    //But option to override this method and use it if necessary.
    public virtual Task ReactiveProcessAsync(Controller controller) => ReactiveProcessAsync();
    public virtual Task ReactiveProcessAsync() => Task.CompletedTask;


    public string Encode() => JsonConvert.SerializeObject(this, Formatting.Indented);

    public virtual string Respond() => Encode();

    protected void TriggerEvent() => ProtocolEvent();
}