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

using System;
using System.Collections.Generic;
using Neo4j.Driver.Internal.IO;

namespace Neo4j.Driver.Internal;
// TODO: Move me.
internal class BeginSessionMessageSerializer : WriteOnlySerializer
{
    public override IEnumerable<Type> WritableTypes => new[] {typeof(BeginSessionMessage)};

    public override void Serialize(PackStreamWriter writer, object value)
    {
        if (!(value is BeginSessionMessage msg))
        {
            throw new ArgumentException(
                $"Can't serialize object of type {value.GetType().Name} as a {nameof(BeginSessionMessage)}");
        }
        writer.WriteStructHeader(1, MessageFormat.MsgBeginSession);
        writer.WriteDictionary(new Dictionary<string, object>
        {
            ["db"] = msg.SessionParameters.Database,
            ["imp_user"] = msg.SessionParameters.ImpersonatedUser,
            ["timeout"] = (int)msg.SessionParameters.Timeout.TotalMilliseconds
        } as IDictionary<string, object>);
    }

    public static readonly BeginSessionMessageSerializer Instance = new();

}
