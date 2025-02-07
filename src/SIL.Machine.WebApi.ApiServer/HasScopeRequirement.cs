﻿using Microsoft.AspNetCore.Authorization;

namespace SIL.Machine.WebApi.ApiServer;

public class HasScopeRequirement : IAuthorizationRequirement
{
    public HasScopeRequirement(string scope, string issuer)
    {
        Scope = scope;
        Issuer = issuer;
    }

    public string Issuer { get; }
    public string Scope { get; }
}
