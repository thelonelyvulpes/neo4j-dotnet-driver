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

namespace Neo4j.Driver.Auth;

public record AuthTokenAndExpiration
{
    public AuthTokenAndExpiration(IAuthToken token, DateTime? expiry = default)
    {
        Token = token;
        Expiry = expiry ?? DateTime.MaxValue;
    }

    public AuthTokenAndExpiration(IAuthToken token, int expiresInMs)
    {
        Token = token;
        Expiry = DateTime.UtcNow.AddMilliseconds(expiresInMs);
    }

    public IAuthToken Token { get; init; }
    public DateTime Expiry { get; init; }

    public void Deconstruct(out IAuthToken Token, out DateTime? Expiry)
    {
        Token = this.Token;
        Expiry = this.Expiry;
    }
}