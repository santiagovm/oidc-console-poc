// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Models;
using System.Collections.Generic;
using Common.SampleConstants;
using IdentityServer4;
using IdentityServer4.Test;

namespace IdentityServer
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new IdentityResource[]
                   {
                       new IdentityResources.OpenId(),
                       new IdentityResources.Profile(),
                   };
        }

        public static IEnumerable<ApiResource> GetApis()
        {
            return new[]
                   {
                       new ApiResource(SampleScopes.TestApi, "My API"),
                   };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new[]
                   {
                       new Client
                       {
                           ClientId = Clients.ConsoleApp,
                           ClientSecrets = { new Secret(Secrets.ConsoleApp.Sha256()) },

                           AllowedGrantTypes = GrantTypes.Code,
                           RequireConsent = false,
                           RequirePkce = true,

                           RedirectUris = { SampleUrls.ConsoleAppCallback },

                           AllowedScopes =
                           {
                               IdentityServerConstants.StandardScopes.OpenId,
                               IdentityServerConstants.StandardScopes.Profile,
                               SampleScopes.TestApi,
                           },
                       },
                   };
        }
        
        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
                   {
                       new TestUser
                       {
                           SubjectId = "1",
                           Username = "alice",
                           Password = "password"
                       },

                       new TestUser
                       {
                           SubjectId = "2",
                           Username = "bob",
                           Password = "password"
                       }
                   };
        }
    }
}
