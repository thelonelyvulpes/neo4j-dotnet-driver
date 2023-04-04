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

using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Auth;

namespace Neo4j.Driver.Internal.Auth;

internal class StaticAuthTokenManager : IAuthTokenManager
{
    private readonly IAuthToken _authToken;

    public StaticAuthTokenManager(IAuthToken authToken)
    {
        _authToken = authToken;
    }

    public Task<IAuthToken> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_authToken);
    }

    public Task OnTokenExpiredAsync(IAuthToken token, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}