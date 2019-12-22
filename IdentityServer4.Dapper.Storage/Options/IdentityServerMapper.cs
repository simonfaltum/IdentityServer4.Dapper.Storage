using System.Collections.Generic;
using System.Security.Claims;
using IdentityServer4.Dapper.Storage.Entities;
using IdentityServer4.Models;
namespace IdentityServer4.Dapper.Storage.Options
{
    public static class IdentityServerMapper
    {
        public static ApiResources MapApiResources(this ApiResource apiResource)
        {
            return new ApiResources()
            {
                Enabled = apiResource.Enabled,
                Name = apiResource.Name,
                DisplayName = apiResource.DisplayName,
                Description = apiResource.Description
            };
        }

        public static ApiResource MapApiResource(this ApiResources apiResources)
        {
            return new ApiResource(name: apiResources.Name, displayName: apiResources.DisplayName)
            {
                Enabled = apiResources.Enabled,
                Description = apiResources.Description
            };
        }

        public static ApiClaims MapApiClaims(this string claim)
        {
            return new ApiClaims()
            {
                Type = claim
            };
        }

        public static string MapApiClaim(this ApiClaims apiClaims)
        {
            return apiClaims.Type;
        }

        public static Secret MapSecret(this ApiSecrets apiSecrets)
        {
            return new Secret(value: apiSecrets.Value, description: apiSecrets.Description, expiration: apiSecrets.Expiration)
            {
                Type = apiSecrets.Type
            };
        }

        public static ApiSecrets MapApiSecret(this Secret secret)
        {
            return new ApiSecrets()
            {
                Description = secret.Description,
                Value = secret.Value,
                Expiration = secret.Expiration,
                Type = secret.Type
            };
        }

        public static Scope MapScope(this ApiScopes apiScopes)
        {
            return new Scope(name: apiScopes.Name, displayName: apiScopes.DisplayName)
            {
                Description = apiScopes.Description,
                Required = apiScopes.Required,
                Emphasize = apiScopes.Emphasize,
                ShowInDiscoveryDocument = apiScopes.ShowInDiscoveryDocument
            };
        }

        public static ApiScopes MapApiScopes(this Scope scope)
        {
            return new ApiScopes()
            {
                Name = scope.Name,
                DisplayName = scope.DisplayName,
                Description = scope.Description,
                Required = scope.Required,
                Emphasize = scope.Emphasize,
                ShowInDiscoveryDocument = scope.ShowInDiscoveryDocument
            };
        }

        public static ApiScopeClaims MapApiScopeClaims(this string claim)
        {
            return new ApiScopeClaims()
            {
                Type = claim
            };
        }

        public static string MapApiScopeClaim(this ApiScopeClaims apiClaims)
        {
            return apiClaims.Type;
        }

        public static ClientProperties MapClientProperties(this KeyValuePair<string, string> keyValuePair)
        {
            return new ClientProperties()
            {
                Key = keyValuePair.Key,
                Value = keyValuePair.Value
            };
        }

        public static KeyValuePair<string, string> MapKeyValuePair(this ClientProperties clientProperties)
        {
            return new KeyValuePair<string, string>(key: clientProperties.Key, value: clientProperties.Value);
        }

        public static Clients MapClients(this Client client)
        {
            return new Clients()
            {
                Enabled = client.Enabled,
                ClientId = client.ClientId,
                ProtocolType = client.ProtocolType,
                RequireClientSecret = client.RequireClientSecret,
                ClientName = client.ClientName,
                Description = client.Description,
                ClientUri = client.ClientUri,
                LogoUri = client.LogoUri,
                RequireConsent = client.RequireConsent,
                AllowRememberConsent = client.AllowRememberConsent,
                AlwaysIncludeUserClaimsInIdToken = client.AlwaysIncludeUserClaimsInIdToken,
                RequirePkce = client.RequirePkce,
                AllowPlainTextPkce = client.AllowPlainTextPkce,
                AllowAccessTokensViaBrowser = client.AllowAccessTokensViaBrowser,
                FrontChannelLogoutUri = client.FrontChannelLogoutUri,
                FrontChannelLogoutSessionRequired = client.FrontChannelLogoutSessionRequired,
                BackChannelLogoutUri = client.BackChannelLogoutUri,
                BackChannelLogoutSessionRequired = client.BackChannelLogoutSessionRequired,
                AllowOfflineAccess = client.AllowOfflineAccess,
                IdentityTokenLifetime = client.IdentityTokenLifetime,
                AccessTokenLifetime = client.AccessTokenLifetime,
                AuthorizationCodeLifetime = client.AuthorizationCodeLifetime,
                ConsentLifetime = client.ConsentLifetime,
                AbsoluteRefreshTokenLifetime = client.AbsoluteRefreshTokenLifetime,
                SlidingRefreshTokenLifetime = client.SlidingRefreshTokenLifetime,
                RefreshTokenUsage = (int)client.RefreshTokenUsage,
                UpdateAccessTokenClaimsOnRefresh = client.UpdateAccessTokenClaimsOnRefresh,
                RefreshTokenExpiration = (int)client.RefreshTokenExpiration,
                AccessTokenType = (int)client.AccessTokenType,
                EnableLocalLogin = client.EnableLocalLogin,
                IncludeJwtId = client.IncludeJwtId,
                AlwaysSendClientClaims = client.AlwaysSendClientClaims,
                ClientClaimsPrefix = client.ClientClaimsPrefix,
                PairWiseSubjectSalt = client.PairWiseSubjectSalt,
                UserSsoLifetime = client.UserSsoLifetime,
                UserCodeType = client.UserCodeType,
                DeviceCodeLifetime = client.DeviceCodeLifetime
            };
        }

        public static Client MapClient(this Clients clients)
        {
            return new Client()
            {
                Enabled = clients.Enabled,
                ClientId = clients.ClientId,
                ProtocolType = clients.ProtocolType,
                RequireClientSecret = clients.RequireClientSecret,
                ClientName = clients.ClientName,
                Description = clients.Description,
                ClientUri = clients.ClientUri,
                LogoUri = clients.LogoUri,
                RequireConsent = clients.RequireConsent,
                AllowRememberConsent = clients.AllowRememberConsent,
                RequirePkce = clients.RequirePkce,
                AllowPlainTextPkce = clients.AllowPlainTextPkce,
                AllowAccessTokensViaBrowser = clients.AllowAccessTokensViaBrowser,
                FrontChannelLogoutUri = clients.FrontChannelLogoutUri,
                FrontChannelLogoutSessionRequired = clients.FrontChannelLogoutSessionRequired,
                BackChannelLogoutUri = clients.BackChannelLogoutUri,
                BackChannelLogoutSessionRequired = clients.BackChannelLogoutSessionRequired,
                AllowOfflineAccess = clients.AllowOfflineAccess,
                AlwaysIncludeUserClaimsInIdToken = clients.AlwaysIncludeUserClaimsInIdToken,
                IdentityTokenLifetime = clients.IdentityTokenLifetime,
                AccessTokenLifetime = clients.AccessTokenLifetime,
                AuthorizationCodeLifetime = clients.AuthorizationCodeLifetime,
                AbsoluteRefreshTokenLifetime = clients.AbsoluteRefreshTokenLifetime,
                SlidingRefreshTokenLifetime = clients.SlidingRefreshTokenLifetime,
                ConsentLifetime = clients.ConsentLifetime,
                RefreshTokenUsage = (TokenUsage)clients.RefreshTokenUsage,
                UpdateAccessTokenClaimsOnRefresh = clients.UpdateAccessTokenClaimsOnRefresh,
                RefreshTokenExpiration = (TokenExpiration)clients.RefreshTokenExpiration,
                AccessTokenType = (AccessTokenType)clients.AccessTokenType,
                EnableLocalLogin = clients.EnableLocalLogin,
                IncludeJwtId = clients.IncludeJwtId,
                AlwaysSendClientClaims = clients.AlwaysSendClientClaims,
                ClientClaimsPrefix = clients.ClientClaimsPrefix,
                PairWiseSubjectSalt = clients.PairWiseSubjectSalt,
                UserSsoLifetime = clients.UserSsoLifetime,
                UserCodeType = clients.UserCodeType,
                DeviceCodeLifetime = clients.DeviceCodeLifetime
            };
        }

        public static string MapCorsOrigin(this ClientCorsOrigins clientCorsOrigins)
        {
            return clientCorsOrigins.Origin;
        }

        public static ClientCorsOrigins MapCorsOrigin(this string origin)
        {
            return new ClientCorsOrigins()
            {
                Origin = origin
            };
        }

        public static string MapClientIdPRestrictions(this ClientIdPRestrictions client)
        {
            return client.Provider;
        }

        public static ClientIdPRestrictions MapClientIdPRestrictions(this string str)
        {
            return new ClientIdPRestrictions()
            {
                Provider = str
            };
        }

        public static ClientClaims MapClientClaims(this Claim claim)
        {
            return new ClientClaims()
            {
                Type = claim.Type,
                Value = claim.Value
            };
        }

        public static Claim MapClaim(this ClientClaims clientClaims)
        {
            return new Claim(type: clientClaims.Type, value: clientClaims.Value);
        }

        public static string MapClientScopes(this ClientScopes client)
        {
            return client.Scope;
        }

        public static ClientScopes MapClientScopes(this string str)
        {
            return new ClientScopes()
            {
                Scope = str
            };
        }

        public static string MapClientPostLogoutRedirectUris(this ClientPostLogoutRedirectUris client)
        {
            return client.PostLogoutRedirectUri;
        }

        public static ClientPostLogoutRedirectUris MapClientPostLogoutRedirectUris(this string str)
        {
            return new ClientPostLogoutRedirectUris()
            {
                PostLogoutRedirectUri = str
            };
        }

        public static string MapClientRedirectUris(this ClientRedirectUris client)
        {
            return client.RedirectUri;
        }

        public static ClientRedirectUris MapClientRedirectUris(this string str)
        {
            return new ClientRedirectUris()
            {
                RedirectUri = str
            };
        }

        public static string MapClientGrantTypes(this ClientGrantTypes client)
        {
            return client.GrantType;
        }

        public static ClientGrantTypes MapClientGrantTypes(this string str)
        {
            return new ClientGrantTypes()
            {
                GrantType = str
            };
        }

        public static string MapIdentityClaims(this IdentityClaims client)
        {
            return client.Type;
        }

        public static IdentityClaims MapIdentityClaims(this string str)
        {
            return new IdentityClaims()
            {
                Type = str
            };
        }

        public static ClientSecrets MapClientSecrets(this Secret secret)
        {
            return new ClientSecrets()
            {
                Description = secret.Description,
                Value = secret.Value,
                Expiration = secret.Expiration,
                Type = secret.Type
            };
        }

        public static Secret MapSecret(this ClientSecrets secret)
        {
            return new Secret(value: secret.Value, description: secret.Description, expiration: secret.Expiration)
            {
                Type = secret.Type
            };
        }

        public static IdentityResource MapIdentityResource(this Entities.IdentityResources identityResources)
        {
            return new IdentityResource()
            {
                Name = identityResources.Name,
                DisplayName = identityResources.DisplayName,
                Required = identityResources.Required,
                Emphasize = identityResources.Emphasize,
                ShowInDiscoveryDocument = identityResources.ShowInDiscoveryDocument,
                Enabled = identityResources.Enabled,
                Description = identityResources.Description
            };
        }

        public static Entities.IdentityResources MapIdentityResources(this IdentityResource identityResource)
        {
            return new Entities.IdentityResources()
            {
                Enabled = identityResource.Enabled,
                Name = identityResource.Name,
                DisplayName = identityResource.DisplayName,
                Description = identityResource.Description,
                Required = identityResource.Required,
                Emphasize = identityResource.Emphasize,
                ShowInDiscoveryDocument = identityResource.ShowInDiscoveryDocument
            };
        }

        public static PersistedGrant MapPersistedGrant(this PersistedGrants persistedGrants)
        {
            return new PersistedGrant()
            {
                Key = persistedGrants.Key,
                Type = persistedGrants.Type,
                SubjectId = persistedGrants.SubjectId,
                ClientId = persistedGrants.ClientId,
                CreationTime = persistedGrants.CreationTime,
                Expiration = persistedGrants.Expiration,
                Data = persistedGrants.Data
            };
        }

        public static PersistedGrants MapPersistedGrants(this PersistedGrant persistedGrant)
        {
            return new PersistedGrants()
            {
                Key = persistedGrant.Key,
                Type = persistedGrant.Type,
                SubjectId = persistedGrant.SubjectId,
                ClientId = persistedGrant.ClientId,
                CreationTime = persistedGrant.CreationTime,
                Expiration = persistedGrant.Expiration,
                Data = persistedGrant.Data
            };
        }
    }
}
