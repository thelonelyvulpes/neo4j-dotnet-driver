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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Preview.Auth;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Auth;

namespace Neo4j.Driver.Tests.TestBackend;

internal abstract class TestAuthTokenManager : IProtocolObject, IAuthTokenManager
{
    public abstract Task<IAuthToken> GetTokenAsync(CancellationToken cancellationToken = default);

    public abstract Task<bool> HandleSecurityExceptionAsync(
        IAuthToken token,
        SecurityException exception,
        CancellationToken cancellationToken = default);
}

internal class NewBasicAuthTokenManager : IProtocolObject
{
    protected Controller _controller;
    public Neo4jAuthTokenManager TokenManager;
    public object data { get; set; }
    
    public override Task Process(Controller controller)
    {
        _controller = controller;
        TokenManager = new Neo4jAuthTokenManager(FakeTime.Instance, GetTokenAsync, typeof(AuthenticationException));
        return Task.CompletedTask;
    }

    public async Task<AuthTokenAndExpiration> GetTokenAsync()
    {
        var requestId = Guid.NewGuid().ToString();
        await _controller.SendResponse(GetAuthRequest(requestId)).ConfigureAwait(false);
        var result = await _controller.TryConsumeStreamObjectOfType<BasicAuthTokenProviderCompleted>()
            .ConfigureAwait(false);

        if (result.data.requestId == requestId)
        {
            var token = new AuthToken(result.data.auth.data.auth.data.ToDictionary());
            var expiresInMs = result.data.auth.data.expiresInMs;
            var expiry = expiresInMs == 0
                ? DateTime.MaxValue
                : FakeTime.Instance.Now().AddMilliseconds(expiresInMs);

            return new AuthTokenAndExpiration(token, expiry);
        }

        throw new Exception("GetTokenAsync: request IDs did not match");
    }

    public override string Respond()
    {
        return new ProtocolResponse("BasicAuthTokenManager", uniqueId).Encode();
    }
    
    protected string GetAuthRequest(string requestId)
    {
        return new ProtocolResponse(
            "BasicAuthTokenProviderRequest",
            new { basicAuthTokenManagerId = uniqueId, id = requestId }).Encode();
    }
}