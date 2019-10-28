
using Dapper;
using IdentityServer4.Dapper.Storage.Entities;
using IdentityServer4.Dapper.Storage.Options;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Dapper.Storage.DataLayer
{
    public class DefaultApiResourceProvider : IApiResourceProvider
    {
        private readonly DBProviderOptions _options;
        private readonly string _connectionString;
        private readonly ILogger<DefaultApiResourceProvider> logger;

        public DefaultApiResourceProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultApiResourceProvider> logger)
        {
            this._options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
            _connectionString = dBProviderOptions.ConnectionString;
            this.logger = logger;
        }

        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var result = connection.QueryFirstOrDefaultAsync<Entities.ApiResources>($"select * from {_options.DbSchema}.ApiResources where Name = @Name", new { Name = name }, commandType: CommandType.Text);
            var api = await result;
                
            if (api != null)
            {
                var apiResource = api.MapApiResource();
                var secrets = await GetSecretByApiResourceId(api.Id);
                if (secrets != null)
                {
                    var apiSecrets = new List<Secret>();
                    secrets.ToList().ForEach(x => apiSecrets.Add(x.MapSecret()));
                    apiResource.ApiSecrets = apiSecrets;
                }
                var scopes = await GetScopesByApiResourceId(api.Id);
                if (scopes != null)
                {
                    var apiScopes = new List<Scope>();
                    scopes.ToList().ForEach(x => apiScopes.Add(x.MapScope()));
                    apiResource.Scopes = apiScopes;
                }
                var apiClaims = await GetClaimsByApiId(api.Id);
                if (apiClaims != null)
                {
                    apiResource.UserClaims = apiClaims.Select(x => x.Type).ToList();
                }

                return apiResource;
            }
            else
            {
                return null;
            }
        }

        public async Task<Entities.ApiResources> GetByName(string name)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var api = connection.QueryFirstOrDefaultAsync<Entities.ApiResources>($"select * from {_options.DbSchema}.ApiResources where Name = @Name", new { Name = name }, commandType: CommandType.Text);
            return await api;
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesAllAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var result = await connection.QueryAsync<Entities.ApiResources>($"select * from {_options.DbSchema}.ApiResources", commandType: CommandType.Text);
            if (result != null && result.Any())
            {
                var apiResourceList = result.ToList();
                var secrets = await connection.QueryAsync<Entities.ApiSecrets>($"select * from {_options.DbSchema}.ApiSecrets", commandType: CommandType.Text);

                var scopes = await connection.QueryAsync<Entities.ApiScopes>($"select * from {_options.DbSchema}.ApiScopes", commandType: CommandType.Text);

                var scopeClaims = await connection.QueryAsync<Entities.ApiScopeClaims>($"select * from {_options.DbSchema}.ApiScopeClaims", commandType: CommandType.Text);


                var apiClaims = await connection.QueryAsync<Entities.ApiClaims>($"select * from {_options.DbSchema}.ApiClaims", commandType: CommandType.Text);

                connection.Close();

                var apiResources = new List<ApiResource>();
                apiResourceList.ForEach(x => apiResources.Add(x.MapApiResource()));


                if (scopes != null && scopes.Any())
                {
                    foreach (var resource in apiResourceList)
                    {
                        var resourceScopes = scopes.Where(x => x.ApiResourceId == resource.Id).ToList();
                        var scopeList = new List<Scope>();
                        resourceScopes.ForEach(x => scopeList.Add(x.MapScope()));

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

                    var userClaimList = (apiClaims.Where(x => x.ApiResourceId == resource.Id).Select(x => x.Type).ToList());
                    var userClaims = apiResources.Single(x => x.Name == resource.Name).UserClaims.Union(userClaimList).ToHashSet();
                    apiResources.Single(x => x.Name == resource.Name).UserClaims = userClaims;


                }

                return apiResources;
            }
            else
            {
                return null;
            }
        }


        public async Task AddAsync(ApiResource apiResource)
        {
            var dbApiResource = await FindApiResourceAsync(apiResource.Name);
            if (dbApiResource != null)
            {
                var resourceName = apiResource.Name;
                logger.LogError("Could not add ApiResource - Reason: Name={resourceName} already exists.", resourceName);
                throw new InvalidOperationException($"ApiResource with Name={apiResource.Name} already exists.");
            }

            var entity = apiResource.MapApiResources();
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();
            //added  Created , Updated, LastAccessed, NonEditable
            var apiId = await con.ExecuteScalarAsync<int>($"insert into {_options.DbSchema}.ApiResources ([Description],DisplayName,Enabled,[Name], Created , Updated, LastAccessed, NonEditable) values (@Description,@DisplayName,@Enabled,@Name, @Created , @Updated, @LastAccessed, @NonEditable);{_options.GetLastInsertID}", new
            {
                entity.Description,
                entity.DisplayName,
                entity.Enabled,
                entity.Name,
                entity.Created,
                entity.Updated,
                entity.LastAccessed,
                entity.NonEditable

            }, commandType: CommandType.Text, transaction: t);
            entity.Id = apiId;

            if (apiResource.UserClaims != null && apiResource.UserClaims.Any())
            {
                foreach (var item in apiResource.UserClaims)
                {
                    await InsertApiResourceClaim(item, entity.Id, con, t);
                }
            }
            if (apiResource.ApiSecrets != null && apiResource.ApiSecrets.Any())
            {
                foreach (var item in apiResource.ApiSecrets)
                {
                    await InsertApiSecretsByApiResourceId(item, apiId, con, t);
                }
            }
            if (apiResource.Scopes != null && apiResource.Scopes.Any())
            {
                await InsertApiScopeByApiResourceId(apiResource.Scopes, entity.Id, con, t);
            }
            t.Commit();
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
                    logger.LogError($"Could not execute delete from ApiResources, return values is {ret}." + " Tried removing name = {name}", name);
                    throw new Exception($"Error executing delete from ApiResources, return values is {ret}");
                }
                await RemoveApiScopeByApiResourceId(entity.Id, con, t);
                await con.ExecuteAsync($"delete from {_options.DbSchema}.ApiClaims where ApiResourceId=@ApiResourceId;delete from ApiSecrets where ApiResourceId=@ApiResourceId;", new
                {
                    ApiResourceId = entity.Id
                }, commandType: CommandType.Text, transaction: t);
                t.Commit();
            }
            catch (Exception ex)
            {
                t.Rollback();
                throw ex;
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
                                                 $"DisplayName=@DisplayName," +
                                                 $"Enabled=@Enabled," +
                                                 $"[Name]=@Name where Id=@Id;", updateEntity, commandType: CommandType.Text, transaction: t);

                await UpdateScopesByApiResourceId(apiResource.Scopes, updateEntity.Id, con, t);
                await UpdateApiSecretsByApiResourceId(apiResource.ApiSecrets, updateEntity.Id, con, t);
                await UpdateClaimsByApiResourceId(apiResource.UserClaims, updateEntity.Id, con, t);
                t.Commit();
            }
            catch (Exception ex)
            {
                t.Rollback();
                throw ex;
            }
        }

        public async Task<IEnumerable<Entities.ApiScopes>> GetScopesByApiResourceId(int apiResourceId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetScopesByApiResourceId(apiResourceId, con, null);
        }

        public async Task<IEnumerable<Entities.ApiScopes>> GetScopesByApiResourceId(int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            var scopes = await con.QueryAsync<Entities.ApiScopes>($"select * from {_options.DbSchema}.ApiScopes where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiResourceId }, commandType: CommandType.Text, transaction: t);
            return scopes;
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null || !scopeNames.Any())
            {
                return null;
            }

            var names = scopeNames.ToArray();
            var listAll = await FindApiResourcesAllAsync();
            return listAll.Where(c => c.Scopes.Any(s => names.Contains(s.Name))).ToList();
        }

        public async Task UpdateScopesByApiResourceIdAsync(ApiResource apiResource)
        {
            var dbItem = await GetByName(apiResource.Name);
            if (dbItem == null)
            {
                var resourceName = apiResource.Name;
                logger.LogError("Could not update ApiResource - Reason: Name={resourceName} not found.", resourceName);
                throw new InvalidOperationException($"Could not update ApiResource {apiResource.Name} - reason: not found");
            }

            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateScopesByApiResourceId(apiResource.Scopes, dbItem.Id, con, t);
                t.Commit();
            }
            catch (Exception ex)
            {
                t.Rollback();
                throw ex;
            }
        }
        private async Task InsertApiScopeByApiResourceId(IEnumerable<Scope> apiScopes, int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            if (apiScopes.IsNullOrEmpty())
            {
                return;
            }
            foreach (var item in apiScopes)
            {
                var scopeId = await con.ExecuteScalarAsync<int>($"insert into {_options.DbSchema}.ApiScopes (ApiResourceId,[Description],DisplayName,Emphasize,[Name],[Required],ShowInDiscoveryDocument) values (@ApiResourceId,@Description,@DisplayName,@Emphasize,@Name,@Required,@ShowInDiscoveryDocument);{_options.GetLastInsertID}", new
                {
                    ApiResourceId = apiResourceId,
                    item.Description,
                    item.DisplayName,
                    item.Emphasize,
                    item.Name,
                    item.Required,
                    item.ShowInDiscoveryDocument
                }, commandType: CommandType.Text, transaction: t);
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
        private async Task RemoveApiScopes(IEnumerable<Entities.ApiScopes> apiScopes, IDbConnection con, IDbTransaction t)
        {
            await con.ExecuteAsync($"delete from {_options.DbSchema}.ApiScopeClaims where ApiScopeId in (@ApiScopeIds);", new
            {
                ApiScopeIds = apiScopes.Select(c => c.Id)
            }, commandType: CommandType.Text, transaction: t);

            await con.ExecuteAsync($"delete from {_options.DbSchema}.ApiScopes where id in (@ApiResourceIds);", new
            {
                ApiResourceIds = apiScopes.Select(c => c.Id)
            }, commandType: CommandType.Text, transaction: t);
        }

        private async Task UpdateScopesByApiResourceId(IEnumerable<Scope> apiScopes, int apiResourceId,
            IDbConnection con, IDbTransaction t)
        {
            if (apiScopes.IsNullOrEmpty())
            {
                await RemoveApiScopeByApiResourceId(apiResourceId, con, t);
            }
            else
            {
                var dbItems = await GetScopesByApiResourceId(apiResourceId, con, t);

                var existingItems = dbItems.ToList();
                var updatedItems = apiScopes.ToList();


                if (existingItems.IsNullOrEmpty())
                {
                    await InsertApiScopeByApiResourceId(updatedItems, apiResourceId, con, t);
                }
                else
                {
                    var newItems = updatedItems.Where(c => !existingItems.Exists(d => d.Name == c.Name));
                    await InsertApiScopeByApiResourceId(newItems, apiResourceId, con, t);
                }

                var deleteItems = existingItems.Where(c => !updatedItems.Exists(d => d.Name == c.Name));
                await RemoveApiScopes(deleteItems, con, t);

                //find updated
                var itemsToUpdate = existingItems.Where(c => updatedItems.Exists(d => d.Name == c.Name));
                if (itemsToUpdate.IsNullOrEmpty())
                {
                    return;
                }

                foreach (var dbItem in itemsToUpdate)
                {
                    var newItem = updatedItems.SingleOrDefault(c => c.Name == dbItem.Name);
                    var updateItem = newItem.MapApiScopes();
                    updateItem.Id = dbItem.Id;
                    //update detail
                    con.Execute(
                        $"update {_options.DbSchema}.ApiScopes set [Description]=@Description,DisplayName=@DisplayName,Emphasize=@Emphasize,[Name]=@Name,[Required]=@Required,ShowInDiscoveryDocument=@ShowInDiscoveryDocument where id=@id;",
                        updateItem, commandType: CommandType.Text, transaction: t);

                }
            }
        }


        public async Task<IEnumerable<Entities.ApiClaims>> GetClaimsByApiId(int apiResourceId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetClaimsByApiId(apiResourceId, con, null);
        }

        public async Task<IEnumerable<Entities.ApiClaims>> GetClaimsByApiId(int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<Entities.ApiClaims>($"select * from {_options.DbSchema}.ApiClaims where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiResourceId }, commandType: CommandType.Text, transaction: t);
        }

        private async Task InsertApiResourceClaim(string item, int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            var ret = await con.ExecuteAsync($"insert into {_options.DbSchema}.ApiClaims (ApiResourceId,[Type]) values (@ApiResourceId,@Type)", new
            {
                ApiResourceId = apiResourceId,
                Type = item
            }, commandType: CommandType.Text, transaction: t);
            if (ret != 1)
            {
               
                logger.LogError("Error executing insert in ApiClaims for apiResourceId {apiResourceId} and type {item}.", apiResourceId, item);
                throw new Exception($"Error executing insert in ApiClaims, return values is {ret}");
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
                    $"delete from {_options.DbSchema}.ApiClaims where ApiResourceId=@ApiResourceId and [Type]=@Type",
                    new { ApiResourceId = apiResourceId, Type = item }, transaction: t);
            }


        }

        public async Task UpdateClaimsByApiResourceIdAsync(ApiResource apiResource)
        {
            var dbItem = await GetByName(apiResource.Name);
            if (dbItem == null)
            {
                logger.LogError("Could not find ApiResource with name {apiResourceName}.", apiResource.Name);
                throw new InvalidOperationException($"Could not find ApiResource {apiResource.Name}");
            }


            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await using var t = await con.BeginTransactionAsync();

            try
            {
                await UpdateClaimsByApiResourceId(apiResource.UserClaims, dbItem.Id, con, t);
                t.Commit();
            }
            catch (Exception ex)
            {
                t.Rollback();
                throw ex;
            }
        }

        public async Task<IEnumerable<Entities.ApiSecrets>> GetSecretByApiResourceId(int apiResourceId)
        {
            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            return await GetSecretByApiResourceId(apiResourceId, con, null);
        }

        private async Task<IEnumerable<Entities.ApiSecrets>> GetSecretByApiResourceId(int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<Entities.ApiSecrets>($"select * from {_options.DbSchema}.ApiSecrets where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiResourceId }, commandType: CommandType.Text, transaction: t);
        }


        private async Task InsertApiSecretsByApiResourceId(Secret item, int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            var ret = await con.ExecuteAsync(
                $"insert into {_options.DbSchema}.ApiSecrets (ApiResourceId,[Description],Expiration,[Type],[Value], Created) values (@ApiResourceId,@Description,@Expiration,@Type,@Value, @Created)",
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
                logger.LogError("Error executing insert in ApiSecrets for apiResourceId {apiResourceId}.", apiResourceId);
                throw new Exception($"Error executing insert in ApiSecrets, return values is {ret}");
            }
        }


        public async Task UpdateApiSecretsByApiResourceId(IEnumerable<Secret> apiSecrets, int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            var dbItems = await GetSecretByApiResourceId(apiResourceId);
            var existingItems = dbItems.ToList();
            var updatedItems = apiSecrets.ToList();


            var removeTheseItems = existingItems.Where(c => updatedItems.All(f => f.Value != c.Value && f.Type != c.Type)).ToList();
            foreach (var item in removeTheseItems)
            {
                await con.ExecuteAsync($"delete from {_options.DbSchema}.{_options.DbSchema}.ApiSecrets where ApiResourceId=@ApiResourceId and [Type]=@Type and [Value]=@Value", new
                {
                    ApiResourceId = apiResourceId,
                    item.Type,
                    Value = item.Value
                }, transaction: t);
            }

            var updateTheseItems = existingItems
                .Where(c => updatedItems.Any(f => f.Value == c.Value && f.Type == c.Type)).ToList();
            foreach (var item in updateTheseItems)
            {
                var updateItem = updatedItems.Single(x => x.Value == item.Value && x.Type == item.Type);
                //update
                await con.ExecuteAsync(
                    $"update {_options.DbSchema}.ApiSecrets set Description=@Description,Expiration=@Expiration,[Type]=@Type,[Value]=@Value where Id=@Id;",
                    new
                    {
                        item.Id,
                        ApiResourceId = apiResourceId,
                        Description = updateItem.Description,
                        updateItem.Expiration,
                        updateItem.Type,
                        Value = updateItem.Value
                    }, commandType: CommandType.Text, transaction: t);
            }
            foreach (var item in updatedItems.Where(c => !existingItems.Exists(d => d.Type == c.Type && d.Value == c.Value)))
            {
                await InsertApiSecretsByApiResourceId(item, apiResourceId, con, t);
            }
        }

        public async Task UpdateApiSecretsByApiResourceIdAsync(ApiResource apiResource)
        {

            var dbItem = await GetByName(apiResource.Name);
            if (dbItem == null)
            {
                logger.LogError("Could not update ApiSecrets for apiResource: {apiResourceName}.", apiResource.Name);
                throw new InvalidOperationException($"Could not update ApiSecrets for apiResource: {apiResource.Name}.");
            }

            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            await UpdateApiSecretsByApiResourceId(apiResource.ApiSecrets, dbItem.Id, null, null);
        }
    }
}
