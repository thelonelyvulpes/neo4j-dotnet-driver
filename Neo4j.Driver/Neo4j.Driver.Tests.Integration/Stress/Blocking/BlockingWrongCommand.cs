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

using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.IntegrationTests.Stress;

public class BlockingWrongCommand<TContext> : BlockingCommand<TContext>
    where TContext : StressTestContext
{
    public BlockingWrongCommand(IDriver driver)
        : base(driver, false)
    {
    }

    public override void Execute(TContext context)
    {
        using (var session = NewSession(AccessMode.Read, context))
        {
            var exc = Record.Exception(() => session.Run("RETURN").Consume());
            exc.Should().BeOfType<ClientException>().Which.Code.Should().Be("Neo.ClientError.Statement.SyntaxError");
        }
    }
}
