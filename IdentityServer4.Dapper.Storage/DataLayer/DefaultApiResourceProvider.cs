
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IdentityServer4.Dapper.Storage.Entities;
using IdentityServer4.Dapper.Storage.Options;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Dapper.Storage.DataLayer
{
    public class DefaultApiResourceProvider : IApiResourceProvider
    {
        private readonly DBProviderOptions _options;
        private readonly string _connectionString;
        private readonly ILogger<DefaultApiResourceProvider> _logger;

        public DefaultApiResourceProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultApiResourceProvider> logger)
        {
            _options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
            _connectionString = dBProviderOptions.ConnectionString;
            this._logger = logger;
        }

        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var result = connection.QueryFirstOrDefaultAsync<ApiResources>($"select * from {_options.DbSchema}.ApiResources where Name = @Name", new { Name = name }, commandType: CommandType.Text);
            var api = await result;

            if (api != null)
            {
                var apiResource = api.MapApiResource();
                var secrets = await GetSecretByApiResourceId(api.Id);
                if (secrets != null)
                {
                    var apiResourceSecrets = new List<Secret>();
                    secrets.ToList().ForEach(x => apiResourceSecrets.Add(x.MapSecret()));
                    apiResource.ApiSecrets = apiResourceSecrets;
                }
                var scopes = await GetScopesByApiResourceId(api.Id);
                if (scopes != null)
                {
                    var apiScopes = new List<string>();
                    scopes.ToList().ForEach(x => apiScopes.Add(x.Scope));
                    apiResource.Scopes = apiScopes;
                }
                var apiResourceClaims = await GetClaimsByApiId(api.Id);
                if (apiResourceClaims != null)
                {
                    apiResource.UserClaims = apiResourceClaims.Select(x => x.Type).ToList();
                }

                return apiResource;
            }

            return null;
        }

        public async Task<ApiResources> GetByName(string name)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var api = connection.QueryFirstOrDefaultAsync<ApiResources>($"select * from {_options.DbSchema}.ApiResources where Name = @Name", new { Name = name }, commandType: CommandType.Text);
            return await api;
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesAllAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var result = await connection.QueryAsync<ApiResources>($"select * from {_options.DbSchema}.ApiResources", commandType: CommandType.Text);
            if (result?.Any() == true)
            {
                var apiResourceList = result.ToList();
                var secrets = await connection.QueryAsync<ApiResourceSecrets>($"select * from {_options.DbSchema}.ApiResourceSecrets", commandType: CommandType.Text);
                
                var scopes = await connection.QueryAsync<ApiResourceScopes>($"select * from {_options.DbSchema}.ApiResourceScopes", commandType: CommandType.Text);

                var scopeClaims = await connection.QueryAsync<ApiScopeClaims>($"select * from {_options.DbSchema}.ApiScopeClaims", commandType: CommandType.Text);

                var apiResourceClaims = await connection.QueryAsync<ApiResourceClaims>($"select * from {_options.DbSchema}.ApiResourceClaims", commandType: CommandType.Text);

                connection.Close();

                var apiResources = new List<ApiResource>();
                apiResourceList.ForEach(x => apiResources.Add(x.MapApiResource()));

                if (scopes?.Any() == true)
                {
                    foreach (var resource in apiResourceList)
                    {
                        var resourceScopes = scopes.Where(x => x.ApiResourceId == resource.Id).ToList();
                        var scopeList = new List<string>();
                        resourceScopes.ForEach(x => scopeList.Add(x.Scope));

                        apiResources.Single(x => x.Name == resource.Name).Scopes = scopeList;

                        foreach (var scope in resourceScopes)
                        {
                            var scopeClaimList = scopeClaims.Where(x => x.ApiScopeId == scope.Id).Select(x => x.Type).ToList();
                            var userClaims = apiResources.Single(x => x.Name == resource.Name).UserClaims.Union(scopeClaimList).ToHashSet();
                            apiResources.Single(x => x.Name == resource.Name).UserClaims = userClaims;
                        }
                    }
                }

                foreach (var resource in apiResourceList)
                {
                    var secretList = new List<Secret>();
                    (secrets.Where(x => x.ApiResourceId == resource.Id).ToList()).ForEach(x => secretList.Add(x.MapSecret()));
                    apiResources.Single(x => x.Name == resource.Name).ApiSecrets = secretList;

                    var userClaimList = (apiResourceClaims.Where(x => x.ApiResourceId == resource.Id).Select(x => x.Type).ToList());
                    var userClaims = apiResources.Single(x => x.Name == resource.Name).UserClaims.Union(userClaimList).ToHashSet();
                    apiResources.Single(x => x.Name == resource.Name).UserClaims = userClaims;
                }

                return apiResources;
            }

            return null;
        }

        public async Task AddAsync(ApiResource apiResource)
        {
            var dbApiResource = await FindApiResourceAsync(apiResource.Name);
            if (dbApiResource != null)
            {
                var resourceName = apiResource.Name;
                _logger.LogError("Could not add ApiResource - Reason: Name={resourceName} already exists.", resourceName);
                throw new InvalidOperationException($"ApiResource with Name={apiResource.Name} already exists.");
            }

            var entity = apiResource.MapApiResources();
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();
            //added  Created , Updated, LastAccessed, NonEditable
            var apiId = await con.ExecuteScalarAsync<int>(
                $"insert into {_options.DbSchema}.ApiResources ([Description],DisplayName,Enabled,[Name], Created , Updated, LastAccessed, NonEditable, AllowedAccessTokenSigningAlgorithms, ShowInDiscoveryDocument) " + 
                $"values (@Description,@DisplayName,@Enabled,@Name, @Created , @Updated, @LastAccessed, @NonEditable, @AllowedAccessTokenSigningAlgorithms, @ShowInDiscoveryDocument);{_options.GetLastInsertID}",
                entity
            , commandType: CommandType.Text, transaction: t);
            entity.Id = apiId;

            if (apiResource.UserClaims?.Any() == true)
            {
                foreach (var item in apiResource.UserClaims)
                {
                    await InsertApiResourceClaim(item, entity.Id, con, t);
                }
            }
            if (apiResource.ApiSecrets?.Any() == true)
            {
                foreach (var item in apiResource.ApiSecrets)
                {
                    await InsertApiResourceSecretsByApiResourceId(item, apiId, con, t);
                }
            }
            if (apiResource.Scopes?.Any() == true)
            {
                await InsertApiScopeByApiResourceId(apiResource.Scopes, entity, con, t);
            }
            await t.CommitAsync();
        }

        public async Task RemoveAsync(string name)
        {
            var entity = await GetByName(name);
            if (entity == null)
            {
                return;
            }

            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();
            try
            {
                var ret = await con.ExecuteAsync($"delete from {_options.DbSchema}.ApiResources where Id=@Id;", new
                {
                    entity.Id
                }, commandType: CommandType.Text, transaction: t);
                if (ret != 1)
                {
                    _logger.LogError($"Could not execute delete from ApiResources, return values is {ret}." + " Tried removing name = {name}", name);
                    throw new Exception($"Error executing delete from ApiResources, return values is {ret}");
                }
                await RemoveApiScopeByApiResourceId(entity.Id, con, t);
                await con.ExecuteAsync($"delete from {_options.DbSchema}.ApiResourceClaims where ApiResourceId=@ApiResourceId;delete from ApiResourceSecrets where ApiResourceId=@ApiResourceId;", new
                {
                    ApiResourceId = entity.Id
                }, commandType: CommandType.Text, transaction: t);
                await t.CommitAsync();
            }
            catch (Exception)
            {
                await t.RollbackAsync();
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        public async Task UpdateAsync(ApiResource apiResource)
        {
            var dbItem = await GetByName(apiResource.Name);
            if (dbItem == null)
            {
                throw new InvalidOperationException($"Can not find ApiResource for Name={apiResource.Name}.");
            }

            var updateEntity = apiResource.MapApiResources();

            updateEntity.Id = dbItem.Id;
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();
            try
            {
                var ret = await con.ExecuteAsync($"update {_options.DbSchema}.ApiResources set [Description] = @Description," +
                                                 "DisplayName=@DisplayName," +
                                                 "Enabled=@Enabled," +
                                                 "[Name]=@Name where Id=@Id;", updateEntity, commandType: CommandType.Text, transaction: t);

                await UpdateScopesByApiResourceId(apiResource.Scopes, updateEntity, con, t);
                await UpdateApiResourceSecretsByApiResourceId(apiResource.ApiSecrets, updateEntity.Id, con, t);
                await UpdateClaimsByApiResourceId(apiResource.UserClaims, updateEntity.Id, con, t);
                await t.CommitAsync();
            }
            catch (Exception)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<ApiResourceScopes>> GetScopesByApiResourceId(int apiResourceId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetScopesByApiResourceId(apiResourceId, con, null);
        }

        public async Task<IEnumerable<ApiResourceScopes>> GetScopesByApiResourceId(int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            var scopes = await con.QueryAsync<ApiResourceScopes>($"select * from {_options.DbSchema}.ApiResourceScopes where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiResourceId }, commandType: CommandType.Text, transaction: t);
            return scopes;
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames?.Any() != true)
            {
                return null;
            }

            var names = scopeNames.ToArray();
            var listAll = await FindApiResourcesAllAsync();
            return listAll.Where(c => c.Scopes.Any(s => names.Contains(s))).ToList();
        }

        public async Task UpdateScopesByApiResourceIdAsync(ApiResource apiResource)
        {
            var dbItem = await GetByName(apiResource.Name);
            if (dbItem == null)
            {
                var resourceName = apiResource.Name;
                _logger.LogError("Could not update ApiResource - Reason: Name={resourceName} not found.", resourceName);
                throw new InvalidOperationException($"Could not update ApiResource {apiResource.Name} - reason: not found");
            }

            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateScopesByApiResourceId(apiResource.Scopes, dbItem, con, t);
                await t.CommitAsync();
            }
            catch (Exception)
            {
                await t.RollbackAsync();
                throw;
            }
        }
        private async Task InsertApiScopeByApiResourceId(IEnumerable<string> apiScopes, ApiResources apiResource, IDbConnection con, IDbTransaction t)
        {
            if (!apiScopes.Any())
            {
                return;
            }
            foreach (var scope in apiScopes)
            {
                var item = new ApiResourceScopes() {Scope = scope, ApiResourceId = apiResource.Id};
                var scopeId = await con.ExecuteScalarAsync<int>($"insert into {_options.DbSchema}.ApiResourceScopes " +
                                                                $"(ApiResourceId,[Scope]) " +
                                                                $"values (@ApiResourceId,@Scope);{_options.GetLastInsertID}", 
                    item, commandType: CommandType.Text, transaction: t);
            }
        }
        private async Task RemoveApiScopeByApiResourceId(int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            await con.ExecuteAsync($"delete from {_options.DbSchema}.ApiScopeClaims where ApiScopeId in (select id from ApiScopes where ApiResourceId=@ApiResourceId);", new
            {
                ApiResourceId = apiResourceId
            }, commandType: CommandType.Text, transaction: t);

            await con.ExecuteAsync($"delete from {_options.DbSchema}.ApiScopes where ApiResourceId=@ApiResourceId;", new
            {
                ApiResourceId = apiResourceId
            }, commandType: CommandType.Text, transaction: t);
        }
        private async Task RemoveApiScopes(IEnumerable<ApiResourceScopes> apiScopes, IDbConnection con, IDbTransaction t)
        {
            await con.ExecuteAsync($"delete from {_options.DbSchema}.ApiResourceScopes where id in (@ApiResourceIds);", new
            {
                ApiResourceIds = apiScopes.Select(c => c.Id)
            }, commandType: CommandType.Text, transaction: t);
        }

        private async Task UpdateScopesByApiResourceId(IEnumerable<string> apiScopes, ApiResources apiResource,
            IDbConnection con, IDbTransaction t)
        {
            if (apiScopes.IsNullOrEmpty())
            {
                await RemoveApiScopeByApiResourceId(apiResource.Id, con, t);
            }
            else
            {
                var dbItems = await GetScopesByApiResourceId(apiResource.Id, con, t);

                var existingItems = dbItems.ToList();
                var updatedItems = apiScopes.ToList();

                if (existingItems.IsNullOrEmpty())
                {
                    await InsertApiScopeByApiResourceId(updatedItems, apiResource, con, t);
                }
                else
                {
                    var newItems = updatedItems.Where(c => !existingItems.Exists(d => d.Scope == c));
                    await InsertApiScopeByApiResourceId(newItems, apiResource, con, t);
                }

                var deleteItems = existingItems.Where(c => !updatedItems.Exists(d => d == c.Scope));
                await RemoveApiScopes(deleteItems, con, t);

                //find updated
                var itemsToUpdate = existingItems.Where(c => updatedItems.Exists(d => d == c.Scope));
                if (itemsToUpdate.IsNullOrEmpty())
                {
                    return;
                }

                foreach (var dbItem in itemsToUpdate)
                {
                    var newItem = updatedItems.SingleOrDefault(c => c == dbItem.Scope);
                    var updateItem = newItem.MapApiScopes();
                    updateItem.Id = dbItem.Id;
                    //update detail
                    con.Execute(
                        $"update {_options.DbSchema}.ApiResourceScopes set [Scope]=@Scope where id=@id;",
                        updateItem, commandType: CommandType.Text, transaction: t);
                }
            }
        }

        public async Task<IEnumerable<ApiResourceClaims>> GetClaimsByApiId(int apiResourceId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetClaimsByApiId(apiResourceId, con, null);
        }

        public async Task<IEnumerable<ApiResourceClaims>> GetClaimsByApiId(int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<ApiResourceClaims>($"select * from {_options.DbSchema}.ApiResourceClaims where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiResourceId }, commandType: CommandType.Text, transaction: t);
        }

        private async Task InsertApiResourceClaim(string item, int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            var ret = await con.ExecuteAsync($"insert into {_options.DbSchema}.ApiResourceClaims (ApiResourceId,[Type]) values (@ApiResourceId,@Type)", new
            {
                ApiResourceId = apiResourceId,
                Type = item
            }, commandType: CommandType.Text, transaction: t);
            if (ret != 1)
            {
                _logger.LogError("Error executing insert in ApiResourceClaims for apiResourceId {apiResourceId} and type {item}.", apiResourceId, item);
                throw new Exception($"Error executing insert in ApiResourceClaims, return values is {ret}");
            }
        }

        public async Task UpdateClaimsByApiResourceId(IEnumerable<string> apiResourceClaims, int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            var dbItems = await GetClaimsByApiId(apiResourceId, con, t);
            var existingItems = dbItems.ToList();
            var updatedItems = apiResourceClaims.ToList();
            foreach (var item in updatedItems)
            {
                if (existingItems.All(x => x.Type != item))
                {
                    await InsertApiResourceClaim(item, apiResourceId, con, t);
                }
            }

            var removeTheseItems = existingItems.Where(c => updatedItems.All(f => f != c.Type)).ToList();
            foreach (var item in removeTheseItems)
            {
                await con.ExecuteAsync(
                    $"delete from {_options.DbSchema}.ApiResourceClaims where ApiResourceId=@ApiResourceId and [Type]=@Type",
                    new { ApiResourceId = apiResourceId, Type = item }, transaction: t);
            }
        }

        public async Task UpdateClaimsByApiResourceIdAsync(ApiResource apiResource)
        {
            var dbItem = await GetByName(apiResource.Name);
            if (dbItem == null)
            {
                _logger.LogError("Could not find ApiResource with name {apiResourceName}.", apiResource.Name);
                throw new InvalidOperationException($"Could not find ApiResource {apiResource.Name}");
            }

            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateClaimsByApiResourceId(apiResource.UserClaims, dbItem.Id, con, t);
                await t.CommitAsync();
            }
            catch (Exception)
            {
                await t.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<ApiResourceSecrets>> GetSecretByApiResourceId(int apiResourceId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetSecretByApiResourceId(apiResourceId, con, null);
        }

        private async Task<IEnumerable<ApiResourceSecrets>> GetSecretByApiResourceId(int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<ApiResourceSecrets>($"select * from {_options.DbSchema}.ApiResourceSecrets where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiResourceId }, commandType: CommandType.Text, transaction: t);
        }

        private async Task InsertApiResourceSecretsByApiResourceId(Secret item, int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            var ret = await con.ExecuteAsync(
                $"insert into {_options.DbSchema}.ApiResourceSecrets (ApiResourceId,[Description],Expiration,[Type],[Value], Created) values (@ApiResourceId,@Description,@Expiration,@Type,@Value, @Created)",
                new
                {
                    ApiResourceId = apiResourceId,
                    item.Description,
                    item.Expiration,
                    item.Type,
                    item.Value,
                    Created = DateTime.UtcNow
                }, commandType: CommandType.Text, transaction: t);
            if (ret != 1)
            {
                _logger.LogError("Error executing insert in ApiResourceSecrets for apiResourceId {apiResourceId}.", apiResourceId);
                throw new Exception($"Error executing insert in ApiResourceSecrets, return values is {ret}");
            }
        }

        public async Task UpdateApiResourceSecretsByApiResourceId(IEnumerable<Secret> apiResourceSecrets, int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            var dbItems = await GetSecretByApiResourceId(apiResourceId);
            var existingItems = dbItems.ToList();
            var updatedItems = apiResourceSecrets.ToList();

            var removeTheseItems = existingItems.Where(c => updatedItems.All(f => f.Value != c.Value && f.Type != c.Type)).ToList();
            foreach (var item in removeTheseItems)
            {
                await con.ExecuteAsync($"delete from {_options.DbSchema}.{_options.DbSchema}.ApiResourceSecrets where ApiResourceId=@ApiResourceId and [Type]=@Type and [Value]=@Value", new
                {
                    ApiResourceId = apiResourceId,
                    item.Type,
                    item.Value
                }, transaction: t);
            }

            var updateTheseItems = existingItems
                .Where(c => updatedItems.Any(f => f.Value == c.Value && f.Type == c.Type)).ToList();
            foreach (var item in updateTheseItems)
            {
                var updateItem = updatedItems.Single(x => x.Value == item.Value && x.Type == item.Type);
                //update
                await con.ExecuteAsync(
                    $"update {_options.DbSchema}.ApiResourceSecrets set Description=@Description,Expiration=@Expiration,[Type]=@Type,[Value]=@Value where Id=@Id;",
                    new
                    {
                        item.Id,
                        ApiResourceId = apiResourceId,
                        updateItem.Description,
                        updateItem.Expiration,
                        updateItem.Type,
                        updateItem.Value
                    }, commandType: CommandType.Text, transaction: t);
            }
            foreach (var item in updatedItems.Where(c => !existingItems.Exists(d => d.Type == c.Type && d.Value == c.Value)))
            {
                await InsertApiResourceSecretsByApiResourceId(item, apiResourceId, con, t);
            }
        }

        public async Task UpdateApiResourceSecretsByApiResourceIdAsync(ApiResource apiResource)
        {
            var dbItem = await GetByName(apiResource.Name);
            if (dbItem == null)
            {
                _logger.LogError("Could not update ApiResourceSecrets for apiResource: {apiResourceName}.", apiResource.Name);
                throw new InvalidOperationException($"Could not update ApiResourceSecrets for apiResource: {apiResource.Name}.");
            }

            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await UpdateApiResourceSecretsByApiResourceId(apiResource.ApiSecrets, dbItem.Id, null, null);
        }
    }
}
