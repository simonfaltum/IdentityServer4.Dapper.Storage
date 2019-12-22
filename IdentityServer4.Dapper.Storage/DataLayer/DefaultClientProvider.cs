
using Dapper;
using IdentityServer4.Dapper.Storage.Entities;
using IdentityServer4.Dapper.Storage.Options;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer4.Dapper.Storage.DataLayer
{

    public class DefaultClientProvider : IClientProvider
    {

        private readonly DBProviderOptions _options;
        private readonly string _connectionString;
        public DefaultClientProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultClientProvider> logger)
        {
            _options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
        
            _connectionString = dBProviderOptions.ConnectionString;
        }

        /// <summary>
        /// find client by client id.
        /// <para>make this method virtual for override in subclass.</para>
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            var clientDto = await GetClientById(clientId);
            if (clientDto == null)
            {
                return null;
            }

            var client = clientDto.MapClient();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var grantTypes = await GetClientGrantTypeByClientId(clientDto.Id);
            var redirectUrls = await GetClientRedirectUriByClientId(clientDto.Id);
            var postLogoutRedirectUris = await GetClientPostLogoutRedirectUriByClientID(clientDto.Id);
            var allowedScopes = await GetClientScopeByClientId(clientDto.Id);
            var secrets = await GetClientSecretByClientId(clientDto.Id);
            var claims = await GetClientClaimByClientId(clientDto.Id);
            var idPRestrictions = await GetClientIdPRestrictionByClientId(clientDto.Id);
            var corsOrigins = await GetClientCorsOriginByClientId(clientDto.Id);
            var properties = await GetClientPropertyByClientId(clientDto.Id);

            if (grantTypes != null)
            {
                client.AllowedGrantTypes = grantTypes.Select(x => x.GrantType).ToList();
            }

            if (redirectUrls != null)
            {
                client.RedirectUris = redirectUrls.Select(x => x.RedirectUri).ToList();
            }

            if (postLogoutRedirectUris != null)
            {
                client.PostLogoutRedirectUris =
                    postLogoutRedirectUris.Select(x => x.PostLogoutRedirectUri).ToList();
            }

            if (allowedScopes != null)
            {
                client.AllowedScopes = allowedScopes.Select(x => x.Scope).ToList();
            }

            if (secrets != null)
            {
                client.ClientSecrets = new List<Secret>();
                secrets.ToList().ForEach(x => client.ClientSecrets.Add(x.MapSecret()));
            }

            if (claims != null)
            {
                client.Claims = new List<Claim>();
                claims.ToList().ForEach(x => client.Claims.Add(x.MapClaim()));
            }

            if (idPRestrictions != null)
            {
                client.IdentityProviderRestrictions = idPRestrictions.Select(x => x.Provider).ToList();
            }

            if (corsOrigins != null)
            {
                client.AllowedCorsOrigins = corsOrigins.Select(x => x.Origin).ToList();
            }

            if (properties != null)
            {
                client.Properties = properties.ToDictionary(x => x.Key, y => y.Value);
            }


            return client;
        }

        public async Task<Clients> GetClientById(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return null;
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<Clients>($"select * from {_options.DbSchema}.Clients where ClientId = @ClientId", new { ClientId = clientId });
        }

        /// <summary>
        /// add the client to db.
        /// <para>clientId will be checked as unique key.</para> 
        /// </summary>
        /// <param name="client"></param>
        public async Task AddAsync(Client client)
        {
            var dbclient = await GetClientById(client.ClientId);
            if (dbclient != null)
            {
                throw new InvalidOperationException($"Client with clientId={client.ClientId} already exist.");
            }
            var clientDto = client.MapClients();

            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var transaction = await con.BeginTransactionAsync();
            try
            {

                var ClientId = await con.ExecuteScalarAsync<int>(
                    $"insert into {_options.DbSchema}.Clients ([AbsoluteRefreshTokenLifetime],[AccessTokenLifetime],[AccessTokenType],[AllowAccessTokensViaBrowser],[AllowOfflineAccess],[AllowPlainTextPkce],[" +
                    $"AllowRememberConsent],[AlwaysIncludeUserClaimsInIdToken],[AlwaysSendClientClaims],[" +
                    $"AuthorizationCodeLifetime],[BackChannelLogoutSessionRequired],[BackChannelLogoutUri],[ClientClaimsPrefix],[ClientId],[ClientName],[ClientUri],[" +
                    $"ConsentLifetime],[Description],[EnableLocalLogin],[Enabled],[FrontChannelLogoutSessionRequired],[FrontChannelLogoutUri],[IdentityTokenLifetime],[IncludeJwtId],[" +
                    $"LogoUri],[PairWiseSubjectSalt],[ProtocolType],[RefreshTokenExpiration],[RefreshTokenUsage],[RequireClientSecret],[RequireConsent],[RequirePkce],[SlidingRefreshTokenLifetime],[" +
                    $"UpdateAccessTokenClaimsOnRefresh],[Created],[Updated],[LastAccessed],[UserSsoLifetime],[UserCodeType],[DeviceCodeLifetime],[NonEditable]) " +
                    $"values (@AbsoluteRefreshTokenLifetime,@AccessTokenLifetime,@AccessTokenType,@AllowAccessTokensViaBrowser,@AllowOfflineAccess,@AllowPlainTextPkce,@AllowRememberConsent," +
                    $"@AlwaysIncludeUserClaimsInIdToken,@AlwaysSendClientClaims,@AuthorizationCodeLifetime,@BackChannelLogoutSessionRequired,@BackChannelLogoutUri,@ClientClaimsPrefix,@ClientId," +
                    $"@ClientName,@ClientUri,@ConsentLifetime,@Description,@EnableLocalLogin,@Enabled,@FrontChannelLogoutSessionRequired,@FrontChannelLogoutUri,@IdentityTokenLifetime,@IncludeJwtId," +
                    $"@LogoUri,@PairWiseSubjectSalt,@ProtocolType,@RefreshTokenExpiration,@RefreshTokenUsage,@RequireClientSecret,@RequireConsent,@RequirePkce,@SlidingRefreshTokenLifetime," +
                    $"@UpdateAccessTokenClaimsOnRefresh,@Created,@Updated,@LastAccessed,@UserSsoLifetime,@UserCodeType,@DeviceCodeLifetime,@NonEditable);{_options.GetLastInsertID}",
                    clientDto, transaction: transaction);
                var result = 0;
                if (client.AllowedGrantTypes != null)
                {
                    foreach (var item in client.AllowedGrantTypes)
                    {
                        result = await con.ExecuteAsync($"insert into {_options.DbSchema}.ClientGrantTypes (ClientId,GrantType) values (@ClientId,@GrantType)", new
                        {
                            ClientId = ClientId,
                            GrantType = item
                        }, transaction: transaction);
                        if (result != 1)
                        {
                            throw new Exception($"Error inserting into ClientGrantTypes, return value is {result}");
                        }
                    }
                }

                if (client.RedirectUris != null)
                {
                    foreach (var item in client.RedirectUris)
                    {
                        result = await con.ExecuteAsync($"insert into {_options.DbSchema}.ClientRedirectUris (ClientId,RedirectUri) values (@ClientId,@RedirectUri)", new
                        {
                            ClientId = ClientId,
                            RedirectUri = item
                        }, transaction: transaction);
                        if (result != 1)
                        {
                            throw new Exception($"Error inserting into ClientRedirectUris, return value is {result}");
                        }
                    }
                }
                if (client.PostLogoutRedirectUris != null)
                {
                    foreach (var item in client.PostLogoutRedirectUris)
                    {
                        result = await con.ExecuteAsync($"insert into {_options.DbSchema}.ClientPostLogoutRedirectUris (ClientId,PostLogoutRedirectUri) values (@ClientId,@PostLogoutRedirectUri)", new
                        {
                            ClientId = ClientId,
                            PostLogoutRedirectUri = item
                        }, transaction: transaction);
                        if (result != 1)
                        {
                            throw new Exception($"Error inserting into ClientPostLogoutRedirectUris, return value is {result}");
                        }
                    }
                }
                if (client.AllowedScopes != null)
                {
                    foreach (var item in client.AllowedScopes)
                    {
                        result = await con.ExecuteAsync($"insert into {_options.DbSchema}.ClientScopes (ClientId,Scope) values (@ClientId,@Scope)", new
                        {
                            ClientId = ClientId,
                            Scope = item
                        }, transaction: transaction);
                        if (result != 1)
                        {
                            throw new Exception($"Error inserting into ClientScopes, return value is {result}");
                        }
                    }
                }
                if (client.ClientSecrets != null)
                {
                    foreach (var item in client.ClientSecrets)
                    {
                        result = await con.ExecuteAsync($"insert into {_options.DbSchema}.ClientSecrets (ClientId,Description,Expiration,[Type],[Value], Created) values (@ClientId,@Description,@Expiration,@Type,@Value, @Created)", new
                        {
                            ClientId = ClientId,
                            Description = item.Description,
                            Expiration = item.Expiration,
                            Type = item.Type,
                            Value = item.Value,
                            Created = clientDto.Created
                        }, transaction: transaction);
                        if (result != 1)
                        {
                            throw new Exception($"Error inserting into ClientSecrets, return value is {result}");
                        }
                    }
                }
                if (client.Claims != null)
                {
                    foreach (var item in client.Claims)
                    {
                        result = await con.ExecuteAsync($"insert into {_options.DbSchema}.ClientClaims ([ClientId],[Type],[Value]) values (@ClientId,@Type,@Value)", new
                        {
                            ClientId = ClientId,
                            Type = item.Type,
                            Value = item.Value
                        }, transaction: transaction);
                        if (result != 1)
                        {
                            throw new Exception($"Error inserting into ClientClaims, return value is {result}");
                        }
                    }
                }
                if (client.IdentityProviderRestrictions != null)
                {
                    foreach (var item in client.IdentityProviderRestrictions)
                    {
                        result = await con.ExecuteAsync($"insert into {_options.DbSchema}.ClientIdPRestrictions (ClientId,Provider) values (@ClientId,@Provider)", new
                        {
                            ClientId = ClientId,
                            Provider = item,
                        }, transaction: transaction);
                        if (result != 1)
                        {
                            throw new Exception($"Error inserting into ClientIdPRestrictions, return value is {result}");
                        }
                    }
                }
                if (client.AllowedCorsOrigins != null)
                {
                    foreach (var item in client.AllowedCorsOrigins)
                    {
                        result = await con.ExecuteAsync($"insert into {_options.DbSchema}.ClientCorsOrigins (ClientId,Origin) values (@ClientId,@Origin)", new
                        {
                            ClientId = ClientId,
                            Origin = item,
                        }, transaction: transaction);
                        if (result != 1)
                        {
                            throw new Exception($"Error inserting into ClientCorsOrigins, return value is {result}");
                        }
                    }
                }
                if (client.Properties != null)
                {
                    foreach (var item in client.Properties)
                    {
                        result = await con.ExecuteAsync($"insert into {_options.DbSchema}.ClientProperties (ClientId,[Key],[Value]) values (@ClientId,@Key,@Value)", new
                        {
                            ClientId,
                            item.Key,
                            item.Value
                        }, transaction: transaction);
                        if (result != 1)
                        {
                            throw new Exception($"Error inserting into ClientProperties, return value is {result}");
                        }
                    }
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<string>> QueryAllowedCorsOriginsAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            var corsOrigins = await connection.QueryAsync<string>($"select distinct(Origin) from {_options.DbSchema}.[ClientCorsOrigins] where Origin is not null", commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            return corsOrigins;
        }


        public async Task RemoveAsync(string clientId)
        {
            var clientEntity = await GetClientById(clientId);
            if (clientEntity == null)
            {
                return;
            }

            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();
            try
            {
                var ret = await con.ExecuteAsync($"delete from {_options.DbSchema}.Clients where id=@id", new { clientEntity.Id }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                ret = await con.ExecuteAsync($"delete from {_options.DbSchema}.ClientGrantTypes where ClientId=@ClientId;" +
                                             $"delete from {_options.DbSchema}.ClientRedirectUris where ClientId=@ClientId;" +
                                             $"delete from {_options.DbSchema}.ClientPostLogoutRedirectUris where ClientId=@ClientId;" +
                                             $"delete from {_options.DbSchema}.ClientScopes where ClientId=@ClientId;" +
                                             $"delete from {_options.DbSchema}.ClientSecrets where ClientId=@ClientId;" +
                                             $"delete from {_options.DbSchema}.ClientClaims where ClientId=@ClientId;" +
                                             $"delete from {_options.DbSchema}.ClientIdPRestrictions where ClientId=@ClientId;" +
                                             $"delete from {_options.DbSchema}.ClientCorsOrigins where ClientId=@ClientId;" +
                                             $"delete from {_options.DbSchema}.ClientProperties where ClientId=@ClientId;", new
                {
                    ClientId = clientEntity.Id
                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw;
            }
        }


        public async Task<IEnumerable<ClientGrantTypes>> GetClientGrantTypeByClientId(int ClientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetClientGrantTypeByClientId(ClientId, con, null);
        }

        private async Task<IEnumerable<ClientGrantTypes>> GetClientGrantTypeByClientId(int ClientId, IDbConnection con, IDbTransaction t)
        {
            var result = await con.QueryAsync<ClientGrantTypes>($"select * from {_options.DbSchema}.ClientGrantTypes where  ClientId = @ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            return result;
        }

        public async Task UpdateClientGrantTypeByClientId(IEnumerable<string> clientGrantTypes, int ClientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateClientGrantTypeByClientId(clientGrantTypes, ClientId, con, t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateClientGrantTypeByClientId(IEnumerable<string> clientGrantTypes, int clientId, IDbConnection con, IDbTransaction t)
        {
            var dbGrants = await GetClientGrantTypeByClientId(clientId, con, t);
            var existingGrants = dbGrants.ToList();
            var updatedGrants = clientGrantTypes.ToList();
            foreach (var grant in updatedGrants)
            {
                if (existingGrants.All(x => x.GrantType != grant))
                {
                    var insertSql = ($"insert into {_options.DbSchema}.ClientGrantTypes (ClientId,GrantType) values (@ClientId,@GrantType)");
                    DynamicParameters insertParameters = new DynamicParameters();
                    insertParameters.Add($"ClientId", clientId);
                    insertParameters.Add($"GrantType", grant);
                    await con.ExecuteAsync(insertSql.ToString(), insertParameters, transaction: t);

                }
            }
            var removeTheseItems = existingGrants.Where(c => !updatedGrants.Exists(d => d == c.GrantType)).Select(c => c.GrantType).ToArray();
            if (!removeTheseItems.IsNullOrEmpty())
            {
                await con.ExecuteAsync(
                    $"delete from {_options.DbSchema}.ClientGrantTypes where ClientId = @ClientId and GrantType in @GrantTypes",
                    new { ClientId = clientId, GrantTypes = removeTheseItems }, transaction: t);
            }

        }

        public async Task<IEnumerable<ClientRedirectUris>> GetClientRedirectUriByClientId(int clientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetClientRedirectUriByClientId(clientId, con, null);
        }

        public async Task<IEnumerable<ClientRedirectUris>> GetClientRedirectUriByClientId(int clientId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<ClientRedirectUris>($"select * from {_options.DbSchema}.ClientRedirectUris where ClientId=@ClientId", new { ClientId = clientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }
        public async Task UpdateClientRedirectUriByClientId(IEnumerable<string> clientRedirectUris, int ClientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateClientRedirectUriByClientId(clientRedirectUris, ClientId, con, t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateClientRedirectUriByClientId(IEnumerable<string> clientRedirectUris, int clientId, IDbConnection con, IDbTransaction t)
        {

            var dbItems = await GetClientRedirectUriByClientId(clientId, con, t);
            var existingItems = dbItems.ToList();
            var updatedItems = clientRedirectUris.ToList();
            foreach (var item in updatedItems)
            {
                if (existingItems.All(x => x.RedirectUri != item))
                {
                    var insertSql = ($"insert into {_options.DbSchema}.ClientRedirectUris (ClientId,RedirectUri) values (@ClientId,@RedirectUri)");
                    DynamicParameters insertParameters = new DynamicParameters();
                    insertParameters.Add($"ClientId", clientId);
                    insertParameters.Add($"RedirectUri", item);
                    await con.ExecuteAsync(insertSql.ToString(), insertParameters, transaction: t);

                }
            }
            var removeTheseItems = existingItems.Where(c => !updatedItems.Exists(d => d == c.RedirectUri)).Select(c => c.RedirectUri).ToArray();
            if (!removeTheseItems.IsNullOrEmpty())
            {
                await con.ExecuteAsync(
                    $"delete from {_options.DbSchema}.ClientRedirectUris where  ClientId = @ClientId and RedirectUri in @RedirectUris",
                    new { ClientId = clientId, RedirectUris = removeTheseItems }, transaction: t);
            }



        }

        public async Task<IEnumerable<Entities.ClientPostLogoutRedirectUris>> GetClientPostLogoutRedirectUriByClientID(int ClientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetClientPostLogoutRedirectUriByClientID(ClientId, con, null);
        }
        public async Task<IEnumerable<Entities.ClientPostLogoutRedirectUris>> GetClientPostLogoutRedirectUriByClientID(int ClientId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<Entities.ClientPostLogoutRedirectUris>($"select * from {_options.DbSchema}.ClientPostLogoutRedirectUris where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }
        public async Task UpdateClientPostLogoutRedirectUriByClientID(IEnumerable<string> clientPostLogoutRedirectUris, int ClientId)
        {
            await using var con = new SqlConnection(_connectionString); 
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateClientPostLogoutRedirectUriByClientID(clientPostLogoutRedirectUris, ClientId, con, t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateClientPostLogoutRedirectUriByClientID(IEnumerable<string> clientPostLogoutRedirectUris, int clientId, IDbConnection con, IDbTransaction t)
        {

            var dbItems = await GetClientPostLogoutRedirectUriByClientID(clientId, con, t);
            var existingItems = dbItems.ToList();
            var updatedItems = clientPostLogoutRedirectUris.ToList();
            foreach (var item in updatedItems)
            {
                if (existingItems.All(x => x.PostLogoutRedirectUri != item))
                {
                    var insertSql = ($"insert into {_options.DbSchema}.ClientPostLogoutRedirectUris (ClientId,PostLogoutRedirectUri) values (@ClientId,@PostLogoutRedirectUri)");
                    DynamicParameters insertParameters = new DynamicParameters();
                    insertParameters.Add($"ClientId", clientId);
                    insertParameters.Add($"PostLogoutRedirectUri", item);
                    await con.ExecuteAsync(insertSql.ToString(), insertParameters, transaction: t);

                }
            }

            var removeTheseItems = existingItems.Where(c => !updatedItems.Exists(d => d == c.PostLogoutRedirectUri)).Select(c => c.PostLogoutRedirectUri).ToArray();
            if (!removeTheseItems.IsNullOrEmpty())
            {
                await con.ExecuteAsync(
                    $"delete from {_options.DbSchema}.ClientPostLogoutRedirectUris where  ClientId = @ClientId and PostLogoutRedirectUri in @PostLogoutRedirectUris",
                    new { ClientId = clientId, PostLogoutRedirectUris = removeTheseItems }, transaction: t);
            }


        }

        public async Task<IEnumerable<Entities.ClientScopes>> GetClientScopeByClientId(int clientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetClientScopeByClientId(clientId, con, null);
        }
        public async Task<IEnumerable<Entities.ClientScopes>> GetClientScopeByClientId(int clientId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<Entities.ClientScopes>($"select * from {_options.DbSchema}.ClientScopes where ClientId=@ClientId", new { ClientId = clientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }
        public async Task UpdateClientScopeByClientId(IEnumerable<string> clientScopes, int clientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateClientScopeByClientId(clientScopes, clientId, con, t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateClientScopeByClientId(IEnumerable<string> clientScopes, int clientId, IDbConnection con, IDbTransaction t)
        {
            var dbItems = await GetClientScopeByClientId(clientId, con, t);
            var existingItems = dbItems.ToList();
            var updatedItems = clientScopes.ToList();
            foreach (var item in updatedItems)
            {
                if (existingItems.All(x => x.Scope != item))
                {
                    var insertSql = ($"insert into {_options.DbSchema}.ClientScopes (ClientId,Scope) values (@ClientId,@Scope)");
                    DynamicParameters insertParameters = new DynamicParameters();
                    insertParameters.Add($"ClientId", clientId);
                    insertParameters.Add($"Scope", item);
                    await con.ExecuteAsync(insertSql.ToString(), insertParameters, transaction: t);

                }
            }

            var removeTheseItems = existingItems.Where(c => updatedItems.All(f => f != c.Scope)).ToArray();
            await con.ExecuteAsync($"delete from {_options.DbSchema}.ClientScopes where  ClientId = @ClientId and Scope in @Scopes", new { ClientId = clientId, Scope = removeTheseItems }, transaction: t);




        }

        public async Task<IEnumerable<Entities.ClientSecrets>> GetClientSecretByClientId(int ClientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetClientSecretByClientId(ClientId, con, null);
        }
        public async Task<IEnumerable<Entities.ClientSecrets>> GetClientSecretByClientId(int ClientId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<Entities.ClientSecrets>($"select * from {_options.DbSchema}.ClientSecrets where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }
        public async Task UpdateClientSecretByClientId(IEnumerable<Secret> clientSecrets, int ClientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateClientSecretByClientId(clientSecrets, ClientId, con, t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateClientSecretByClientId(IEnumerable<Secret> clientSecrets, int clientId, IDbConnection con, IDbTransaction t)
        {
            var dbItems = await GetClientSecretByClientId(clientId, con, t);
            var existingItems = dbItems.ToList();
            var updatedItems = clientSecrets.ToList();
            foreach (var item in updatedItems)
            {
                if (existingItems.All(x => x.Type != item.Type && x.Value != item.Value))
                {
                    var insertSql = ($"insert into {_options.DbSchema}.ClientSecrets (ClientId,Description,Expiration,Type,Value, Created) values (@ClientId,@Description,@Expiration,@Type,@Value, @Created)");
                    DynamicParameters insertParameters = new DynamicParameters();
                    insertParameters.Add($"ClientId", clientId);
                    insertParameters.Add($"Description", item.Description);
                    insertParameters.Add($"Expiration", item.Expiration);
                    insertParameters.Add($"Type", item.Type);
                    insertParameters.Add($"Value", item.Value);
                    insertParameters.Add($"Created", DateTimeOffset.UtcNow);
                    await con.ExecuteAsync(insertSql.ToString(), insertParameters, transaction: t);

                }
            }

            var removeTheseItems = existingItems.Where(c => updatedItems.All(f => f.Value != c.Value && f.Type != c.Type)).ToList();
            foreach (var item in removeTheseItems)
            {
                await con.ExecuteAsync($"delete from {_options.DbSchema}.ClientSecrets where ClientId = @ClientId and [Value] = @Value and [Type]=@Type", new { ClientId = clientId, Value = item.Value, item.Type }, transaction: t);
            }
        }


        public async Task<IEnumerable<ClientClaims>> GetClientClaimByClientId(int clientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetClientClaimByClientId(clientId, con, null);
        }
        public async Task<IEnumerable<ClientClaims>> GetClientClaimByClientId(int clientId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<ClientClaims>($"select * from {_options.DbSchema}.ClientClaims where ClientId=@ClientId", new
            {
                ClientId = clientId
            }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }
        public async Task UpdateClientClaimByClientId(IEnumerable<Claim> clientClaims, int clientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateClientClaimByClientId(clientClaims, clientId, con, t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateClientClaimByClientId(IEnumerable<Claim> clientClaims, int clientId, IDbConnection con, IDbTransaction t)
        {
            var dbItems = await GetClientSecretByClientId(clientId, con, t);
            var existingItems = dbItems.ToList();
            var updatedItems = clientClaims.ToList();
            foreach (var item in updatedItems)
            {
                if (existingItems.All(x => x.Type != item.Type && x.Value != item.Value))
                {
                    var insertSql = ($"insert into {_options.DbSchema}.ClientClaims (ClientId,Type,Value) values (@ClientId,@Type,@Value)");
                    DynamicParameters insertParameters = new DynamicParameters();
                    insertParameters.Add($"ClientId", clientId);
                    insertParameters.Add($"Type", item.Type);
                    insertParameters.Add($"Value", item.Value);
                    await con.ExecuteAsync(insertSql.ToString(), insertParameters, transaction: t);

                }
            }
            var removeTheseItems = existingItems.Where(c => updatedItems.All(f => f.Value != c.Value && f.Type != c.Type)).ToList();
            foreach (var item in removeTheseItems)
            {
                await con.ExecuteAsync($"delete from {_options.DbSchema}.ClientClaims where ClientId = @ClientId and [Value] = @Value and [Type]=@Type;", new { ClientId = clientId, Value = item.Value, item.Type }, transaction: t);
            }

        }

        public async Task<IEnumerable<Entities.ClientIdPRestrictions>> GetClientIdPRestrictionByClientId(int clientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetClientIdPRestrictionByClientId(clientId, con, null);
        }
        public async Task<IEnumerable<Entities.ClientIdPRestrictions>> GetClientIdPRestrictionByClientId(int clientId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<Entities.ClientIdPRestrictions>($"select * from {_options.DbSchema}.ClientIdPRestrictions where ClientId=@ClientId", new { ClientId = clientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }

        public async Task UpdateClientIdPRestrictionByClientId(IEnumerable<string> clientIdPRestrictions, int clientId)
        {
            await using var con = new SqlConnection(_connectionString); 
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateClientIdPRestrictionByClientId(clientIdPRestrictions, clientId, con, t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateClientIdPRestrictionByClientId(IEnumerable<string> clientIdPRestrictions, int clientId, IDbConnection con, IDbTransaction t)
        {

            var dbItems = await GetClientIdPRestrictionByClientId(clientId, con, t);
            var existingItems = dbItems.ToList();
            var updatedItems = clientIdPRestrictions.ToList();
            foreach (var item in updatedItems)
            {
                if (existingItems.All(x => x.Provider != item))
                {
                    var insertSql = ($"insert into {_options.DbSchema}.ClientIdPRestrictions (ClientId,Provider) values (@ClientId,@Provider)");
                    DynamicParameters insertParameters = new DynamicParameters();
                    insertParameters.Add($"ClientId", clientId);
                    insertParameters.Add($"Provider", item);
                    await con.ExecuteAsync(insertSql.ToString(), insertParameters, transaction: t);

                }
            }

            var removeTheseItems = existingItems.Where(c => !updatedItems.Exists(d => d == c.Provider)).Select(c => c.Provider).ToArray();
            if (!removeTheseItems.IsNullOrEmpty())
            {
                await con.ExecuteAsync(
                    $"delete from {_options.DbSchema}.ClientIdPRestrictions where  ClientId = @ClientId and Provider in @Providers",
                    new { ClientId = clientId, Providers = removeTheseItems }, transaction: t);
            }

        }

        public async Task<IEnumerable<ClientCorsOrigins>> GetClientCorsOriginByClientId(int clientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetClientCorsOriginByClientId(clientId, con, null);
        }
        public async Task<IEnumerable<ClientCorsOrigins>> GetClientCorsOriginByClientId(int clientId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<ClientCorsOrigins>($"select * from {_options.DbSchema}.ClientCorsOrigins where ClientId=@ClientId", new { ClientId = clientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }

        public async Task UpdateClientCorsOriginByClientId(IEnumerable<string> clientCorsOrigins, int clientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateClientCorsOriginByClientId(clientCorsOrigins, clientId, con, t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateClientCorsOriginByClientId(IEnumerable<string> clientCorsOrigins, int clientId, IDbConnection con, IDbTransaction t)
        {
            var dbItems = await GetClientCorsOriginByClientId(clientId, con, t);
            var existingItems = dbItems.ToList();
            var updatedItems = clientCorsOrigins.ToList();
            foreach (var item in updatedItems)
            {
                if (existingItems.All(x => x.Origin != item))
                {
                    var insertSql = ($"insert into {_options.DbSchema}.ClientCorsOrigins (ClientId,Origin) values (@ClientId,@Origin)");
                    DynamicParameters insertParameters = new DynamicParameters();
                    insertParameters.Add($"ClientId", clientId);
                    insertParameters.Add($"Origin", item);
                    await con.ExecuteAsync(insertSql.ToString(), insertParameters, transaction: t);

                }
            }

            var removeTheseItems = existingItems.Where(c => !updatedItems.Exists(d => d == c.Origin)).Select(c => c.Origin).ToArray();
            if (!removeTheseItems.IsNullOrEmpty())
            {
                await con.ExecuteAsync(
                    $"delete from {_options.DbSchema}.ClientCorsOrigins  where  ClientId = @ClientId and Origin in @Origins",
                    new { ClientId = clientId, Origins = removeTheseItems }, transaction: t);
            }



        }

        public async Task<IEnumerable<ClientProperties>> GetClientPropertyByClientId(int clientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetClientPropertyByClientId(clientId, con, null);
        }
        public async Task<IEnumerable<ClientProperties>> GetClientPropertyByClientId(int clientId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<ClientProperties>($"select * from {_options.DbSchema}.ClientProperties where ClientId=@ClientId", new { ClientId = clientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }

        public async Task UpdateClientPropertyByClientId(IDictionary<string, string> clientProperties, int ClientId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateClientPropertyByClientId(clientProperties, ClientId, con, t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateClientPropertyByClientId(IDictionary<string, string> clientProperties, int clientId, IDbConnection con, IDbTransaction t)
        {
            var dbItems = await GetClientPropertyByClientId(clientId, con, t);
            var existingItems = dbItems.ToList();
            var updatedItems = clientProperties;
            foreach (var item in updatedItems)
            {
                if (existingItems.All(x => x.Key != item.Key && x.Value != item.Value))
                {
                    var insertSql = ($"insert into {_options.DbSchema}.ClientProperties (ClientId,[Key],[Value]) values (@ClientId,@Key,@Value)");
                    DynamicParameters insertParameters = new DynamicParameters();
                    insertParameters.Add($"ClientId", clientId);
                    insertParameters.Add($"Key", item.Key);
                    insertParameters.Add($"Value", item.Value);

                    await con.ExecuteAsync(insertSql.ToString(), insertParameters, transaction: t);

                }
            }

            foreach (var item in existingItems)
            {
                if (!updatedItems.ContainsKey(item.Key) && updatedItems[item.Key] != item.Value)
                {
                    await con.ExecuteAsync(
                        $"delete from {_options.DbSchema}.ClientProperties where ClientId = @ClientId and [Value] = @Value and [Key]=@Key",
                        new { ClientId = clientId, Value = item.Value, item.Key }, transaction: t);
                }
            }
        }

        public async Task UpdateAsync(Client client)
        {
            var dbClient = await GetClientById(client.ClientId);
            if (dbClient == null)
            {
                throw new InvalidOperationException($"Can not find client for clientId={client.ClientId}.");
            }

            var entity = client.MapClients();

            entity.Id = dbClient.Id;
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();
            try
            {
                var ret = await con.ExecuteAsync($"update {_options.DbSchema}.Clients set AbsoluteRefreshTokenLifetime = @AbsoluteRefreshTokenLifetime," +
                                                 $"AccessTokenLifetime=@AccessTokenLifetime," +
                                                 $"AccessTokenType=@AccessTokenType," +
                                                 $"AllowAccessTokensViaBrowser=@AllowAccessTokensViaBrowser," +
                                                 $"AllowOfflineAccess=@AllowOfflineAccess," +
                                                 $"AllowPlainTextPkce=@AllowPlainTextPkce," +
                                                 $"AllowRememberConsent=@AllowRememberConsent," +
                                                 $"AlwaysIncludeUserClaimsInIdToken=@AlwaysIncludeUserClaimsInIdToken," +
                                                 $"AlwaysSendClientClaims=@AlwaysSendClientClaims," +
                                                 $"AuthorizationCodeLifetime=@AuthorizationCodeLifetime," +
                                                 $"BackChannelLogoutSessionRequired=@BackChannelLogoutSessionRequired," +
                                                 $"BackChannelLogoutUri=@BackChannelLogoutUri," +
                                                 $"ClientClaimsPrefix=@ClientClaimsPrefix," +
                                                 $"ClientName=@ClientName," +
                                                 $"ClientUri=@ClientUri," +
                                                 $"ConsentLifetime=@ConsentLifetime," +
                                                 $"Description=@Description," +
                                                 $"EnableLocalLogin=@EnableLocalLogin," +
                                                 $"Enabled=@Enabled," +
                                                 $"FrontChannelLogoutSessionRequired=@FrontChannelLogoutSessionRequired," +
                                                 $"FrontChannelLogoutUri=@FrontChannelLogoutUri," +
                                                 $"IdentityTokenLifetime=@IdentityTokenLifetime," +
                                                 $"IncludeJwtId=@IncludeJwtId," +
                                                 $"LogoUri=@LogoUri," +
                                                 $"PairWiseSubjectSalt=@PairWiseSubjectSalt," +
                                                 $"ProtocolType=@ProtocolType," +
                                                 $"RefreshTokenExpiration=@RefreshTokenExpiration," +
                                                 $"RefreshTokenUsage=@RefreshTokenUsage," +
                                                 $"RequireClientSecret=@RequireClientSecret," +
                                                 $"RequireConsent=@RequireConsent," +
                                                 $"RequirePkce=@RequirePkce," +
                                                 $"SlidingRefreshTokenLifetime=@SlidingRefreshTokenLifetime," +
                                                 $"UpdateAccessTokenClaimsOnRefresh=@UpdateAccessTokenClaimsOnRefresh where ClientId=@ClientId;", entity, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);

                await UpdateClientGrantTypeByClientId(client.AllowedGrantTypes, entity.Id, con, t);
                await UpdateClientRedirectUriByClientId(client.RedirectUris, entity.Id, con, t);
                await UpdateClientPostLogoutRedirectUriByClientID(client.PostLogoutRedirectUris, entity.Id, con, t);
                await UpdateClientScopeByClientId(client.AllowedScopes, entity.Id, con, t);
                await UpdateClientSecretByClientId(client.ClientSecrets, entity.Id, con, t);
                await UpdateClientClaimByClientId(client.Claims, entity.Id, con, t);
                await UpdateClientIdPRestrictionByClientId(client.IdentityProviderRestrictions, entity.Id, con, t);
                await UpdateClientCorsOriginByClientId(client.AllowedCorsOrigins, entity.Id, con, t);
                await UpdateClientPropertyByClientId(client.Properties, entity.Id, con, t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        private async Task<Clients> GetClientEntity(Client client)
        {
            var dbClient = await GetClientById(client.ClientId);
            if (dbClient == null)
            {
                throw new InvalidOperationException($"Can not find client for clientId={client.ClientId}.");
            }

            var entity = client.MapClients();
            entity.Id = dbClient.Id;
            return entity;
        }

        public async Task UpdateGrantTypesAsync(Client client)
        {
            var entity = await GetClientEntity(client);
            await UpdateClientGrantTypeByClientId(client.AllowedGrantTypes, entity.Id);
        }

        public async Task UpdateRedirectUrisAsync(Client client)
        {
            var entity = await GetClientEntity(client);
            await UpdateClientRedirectUriByClientId(client.RedirectUris, entity.Id);
        }

        public async Task UpdatePostLogoutRedirectUrisAsync(Client client)
        {
            var entity = await GetClientEntity(client);
            await UpdateClientPostLogoutRedirectUriByClientID(client.PostLogoutRedirectUris, entity.Id);
        }

        public async Task UpdateScopesAsync(Client client)
        {
            var entity = await GetClientEntity(client);
            await UpdateClientScopeByClientId(client.AllowedScopes, entity.Id);
        }

        public async Task UpdateSecretsAsync(Client client)
        {
            var entity = await GetClientEntity(client);
            await UpdateClientSecretByClientId(client.ClientSecrets, entity.Id);
        }

        public async Task UpdateClaimsAsync(Client client)
        {
            var entity = await GetClientEntity(client);
            await UpdateClientClaimByClientId(client.Claims, entity.Id);
        }

        public async Task UpdateIdPRestrictionsAsync(Client client)
        {
            var entity = await GetClientEntity(client);
            await UpdateClientIdPRestrictionByClientId(client.IdentityProviderRestrictions, entity.Id);
        }

        public async Task UpdateCorsOriginsAsync(Client client)
        {
            var entity = await GetClientEntity(client);
            await UpdateClientCorsOriginByClientId(client.AllowedCorsOrigins, entity.Id);
        }

        public async Task UpdatePropertiesAsync(Client client)
        {
            var entity = await GetClientEntity(client);
            await UpdateClientPropertyByClientId(client.Properties, entity.Id);
        }

    }
}
