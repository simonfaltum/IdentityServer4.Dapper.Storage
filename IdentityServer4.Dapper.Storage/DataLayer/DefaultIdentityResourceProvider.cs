using AutoMapper;
using Dapper;
using IdentityServer4.Dapper.Storage.Options;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer4.Dapper.Storage.DataLayer
{
    public class DefaultIdentityResourceProvider : IIdentityResourceProvider
    {
        private DBProviderOptions _options;
        private readonly IMapper _mapper;
        private readonly string _connectionString;
        public DefaultIdentityResourceProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultIdentityResourceProvider> logger)
        {
            this._options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
            _mapper = new MapperConfiguration(cfg => cfg.AddProfile<IdServerDapperMapperProfile>())
                .CreateMapper();
            _connectionString = dBProviderOptions.ConnectionString;
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesAllAsync()
        {
            var results = new List<IdentityResource>();
            using (var connection = new SqlConnection(_connectionString))
            {
                var resources = await connection.QueryAsync<Entities.IdentityResources>($"select * from {_options.DbSchema}.IdentityResources", commandType: CommandType.Text);
                var claims = await connection.QueryAsync<Entities.IdentityClaims>($"select * from {_options.DbSchema}.IdentityClaims");
                if (resources != null)
                {
                    foreach (var resource in resources)
                    {
                        var model = _mapper.Map<IdentityResource>(resource);
                        model.UserClaims = claims.Where(x => x.IdentityResourceId == resource.Id).Select(x => x.Type).ToList();
                        results.Add(model);
                    }
                }
                return results;
            }
        }

        public async Task<IdentityResource> FindIdentityResourcesByNameAsync(string name)
        {
            var identityResource = await GetByName(name);
            if (identityResource == null)
                return null;

            var claims = await GetClaimsByIdentityId(identityResource.Id);
            var model = _mapper.Map<IdentityResource>(identityResource);
            model.UserClaims = claims.Select(x => x.Type).ToList();
            return model;

        }

        private async Task<Entities.IdentityResources> GetByName(string name)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var result = await connection.QuerySingleOrDefaultAsync<Entities.IdentityResources>(
                    $"select * from {_options.DbSchema}.IdentityResources where Name = @Name", new { Name = name },
                     commandType: CommandType.Text);
                return result;
            }
        }

        private async Task<IEnumerable<Entities.IdentityClaims>> GetClaimsByName(string identityName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<Entities.IdentityClaims>($"select claim.* from {_options.DbSchema}.IdentityClaims claim inner join {_options.DbSchema}.IdentityResources ic on claim.IdentityResourceId = ic.id where ic.Name = @Name", new { Name = identityName }, commandType: CommandType.Text);
            }
        }

        private async Task<IEnumerable<Entities.IdentityClaims>> GetClaimsByIdentityId(int identityId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return await GetClaimsByIdentityId(identityId, connection, null);
            }
        }

        private async Task<IEnumerable<Entities.IdentityClaims>> GetClaimsByIdentityId(int identityId, IDbConnection con, IDbTransaction t)
        {
            return await con.QueryAsync<Entities.IdentityClaims>($"select * from {_options.DbSchema}.IdentityClaims claim where claim.IdentityResourceId = @Id", new { Id = identityId }, commandType: CommandType.Text, transaction: t);
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null || !scopeNames.Any())
            {
                return null;
            }

            if (scopeNames.Count() > 20)
            {
                var listAll = await FindIdentityResourcesAllAsync();
                return listAll.Where(c => scopeNames.Contains(c.Name));
            }
            else
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    DynamicParameters parameters = new DynamicParameters();
                    StringBuilder conditions = new StringBuilder();
                    int index = 1;
                    foreach (var item in scopeNames)
                    {
                        if (string.IsNullOrWhiteSpace(item))
                        {
                            continue;
                        }
                        conditions.Append($"@Scope{index},");
                        parameters.Add($"@Scope{index}", item);
                        index++;
                    }

                    string sql = $"select * from {_options.DbSchema}.IdentityResources ic where ic.Name in ({conditions.ToString().TrimEnd(',')})";
                    var task = connection.QueryAsync<Entities.IdentityResources>(sql, parameters);

                    var result = await task;
                    var identityResources = _mapper.Map<List<IdentityResource>>(result);
                    foreach (var resource in identityResources)
                    {
                        var claims = await GetClaimsByName(resource.Name);
                        resource.UserClaims = claims.Select(x => x.Type).ToList();
                    }

                    return identityResources;
                }
            }
        }

        public async Task AddAsync(IdentityResource identityResource)
        {
            var dbIdentityResource = await FindIdentityResourcesByNameAsync(identityResource.Name);
            if (dbIdentityResource != null)
            {
                throw new InvalidOperationException($"Found identityResource with Name={dbIdentityResource.Name} already exists.");
            }

            var entity = _mapper.Map<Entities.IdentityResources>(identityResource);
            using (var con = new SqlConnection(_connectionString))
            {

                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var identityResourceId = await con.ExecuteScalarAsync<int>($"insert into {_options.DbSchema}.IdentityResources ([Description],DisplayName,Emphasize,Enabled,[Name],Required,ShowInDiscoveryDocument, Created,Updated,NonEditable) values (@Description,@DisplayName,@Emphasize,@Enabled,@Name,@Required,@ShowInDiscoveryDocument, @Created,@Updated,@NonEditable);{_options.GetLastInsertID}", new
                        {
                            entity.Description,
                            entity.DisplayName,
                            entity.Emphasize,
                            entity.Enabled,
                            entity.Name,
                            entity.Required,
                            entity.ShowInDiscoveryDocument,
                            entity.Created,
                            entity.Updated,
                            entity.NonEditable


                        }, commandType: CommandType.Text, transaction: t);

                        entity.Id = identityResourceId;
                        if (identityResource.UserClaims != null && identityResource.UserClaims.Count() > 0)
                        {
                            foreach (var item in identityResource.UserClaims)
                            {
                                await InsertApiResourceClaim(item, entity.Id, con, t);
                            }
                        }

                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
            }
        }


        public async Task RemoveAsync(string name)
        {
            var entity = await GetByName(name);
            if (entity == null)
            {
                return;
            }
            using (var con = new SqlConnection(_connectionString))
            {

                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ret = await con.ExecuteAsync($"delete from {_options.DbSchema}.IdentityClaims where IdentityResourceId=@IdentityResourceId;", new
                        {
                            IdentityResourceId = entity.Id
                        }, commandType: CommandType.Text, transaction: t);
                        ret = await con.ExecuteAsync($"delete from {_options.DbSchema}.IdentityResources where id=@id", new { entity.Id }, commandType: CommandType.Text, transaction: t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public async Task UpdateAsync(IdentityResource identityResource)
        {
            var dbItem = GetByName(identityResource.Name);
            if (dbItem == null)
            {
                throw new InvalidOperationException($"Could not find IdentityResource with name {identityResource.Name}. Update failed.");
            }

            var entity = _mapper.Map<Entities.IdentityResources>(identityResource);

            entity.Id = dbItem.Id;
            using (var con = new SqlConnection(_connectionString))
            {

                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ret = await con.ExecuteAsync($"update {_options.DbSchema}.IdentityResources set [Description] = @Description," +
                            $"DisplayName=@DisplayName," +
                            $"Enabled=@Enabled," +
                            $"Emphasize=@Emphasize," +
                            $"[Name]=@Name," +
                            $"Required=@Required," +
                            $"ShowInDiscoveryDocument=@ShowInDiscoveryDocument where Id=@Id;", entity, commandType: CommandType.Text, transaction: t);
                        await UpdateClaims(identityResource.UserClaims, entity.Id, con, t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
            }

        }

        public async Task UpdateClaimsAsync(IdentityResource identityResource)
        {
            var dbItem = GetByName(identityResource.Name);
            if (dbItem == null)
            {
                throw new InvalidOperationException($"Could not find IdentityResource with name {identityResource.Name}. Update failed.");
            }

            using (var con = new SqlConnection(_connectionString))
            {
                await UpdateClaims(identityResource.UserClaims, dbItem.Id, con, null);
            }
        }

        private async Task UpdateClaims(IEnumerable<string> identityClaims, int identityId, IDbConnection con, IDbTransaction t)
        {
            var dbItems = await GetClaimsByIdentityId(identityId, con, t);
            var existingItems = dbItems.ToList();
            var updatedItems = identityClaims.ToList();
            foreach (var item in updatedItems)
            {
                if (!existingItems.Exists(x => x.Type == item))
                {
                    await InsertApiResourceClaim(item, identityId, con, t);
                }
            }

            var removeTheseItems = existingItems.Where(c => !updatedItems.Exists(d => d == c.Type)).Select(c => c.Type).ToArray();
            foreach (var item in removeTheseItems)
            {
                await con.ExecuteAsync($"delete from {_options.DbSchema}.IdentityClaims where IdentityResourceId=@IdentityResourceId and [Type]=@Type;", new
                {
                    IdentityResourceId = identityId,
                    Type = item
                }, commandType: CommandType.Text, transaction: t);
            }
        }

        private async Task InsertApiResourceClaim(string item, int identityId, IDbConnection con, IDbTransaction t)
        {
            var ret = await con.ExecuteAsync($"insert into {_options.DbSchema}.IdentityClaims ([IdentityResourceId],[Type]) values (@identityResourceId,@Type)", new
            {
                identityResourceId = identityId,
                Type = item
            }, commandType: CommandType.Text, transaction: t);
            if (ret != 1)
            {
                throw new Exception($"Error inserting into IdentityClaims, return value is {ret}");
            }
        }
    }
}
