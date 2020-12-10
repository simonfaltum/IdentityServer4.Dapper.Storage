using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IdentityServer4.Dapper.Storage.Entities;
using IdentityServer4.Dapper.Storage.Options;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Dapper.Storage.DataLayer
{
    public class DefaultApiScopesProvider : IApiScopesProvider
    {
        private readonly DBProviderOptions _options;
        private readonly string _connectionString;
        private readonly ILogger<DefaultApiScopesProvider> logger;

        public DefaultApiScopesProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultApiScopesProvider> logger)
        {
            _options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
            _connectionString = dBProviderOptions.ConnectionString;
            this.logger = logger;
        }

        public async Task<ApiScope> FindApiScopeByNameAsync(string name)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var result =
                await connection.QueryFirstOrDefaultAsync<ApiScopes>(
                    $"select * from {_options.DbSchema}.ApiScopes where Name = @Name", new {Name = name});
            return result == null ? null : result.MapScope();
        }
            

        public async Task RemoveApiScopes(IEnumerable<ApiScopes> apiScopes)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var t = await connection.BeginTransactionAsync();
            try
            {
                await connection.ExecuteAsync(
                    $"delete from {_options.DbSchema}.ApiScopeClaims where ApiScopeId in (@ApiScopeIds);", new
                    {
                        ApiScopeIds = apiScopes.Select(c => c.Id)
                    }, commandType: CommandType.Text, transaction: t);

                await connection.ExecuteAsync($"delete from {_options.DbSchema}.ApiScopes where id in (@Ids);", new
                {
                    Ids = apiScopes.Select(c => c.Id)
                }, commandType: CommandType.Text, transaction: t);
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                throw ex;
            }
        }

        public async Task InsertNewApiScope(ApiScope apiScope)
        {
            var apiScopeEntity = apiScope.MapApiScopes();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var query =
                $"insert into {_options.DbSchema}.ApiScopes (Name, DisplayName, Description, Required, Emphasize, ShowInDiscoveryDocument, Enabled)" +
                $" VALUES (@Name, @DisplayName, @Description, @Required, @Emphasize, @ShowInDiscoveryDocument, @Enabled)";
            await connection.ExecuteAsync(query, apiScopeEntity);
        }

        public async Task<IEnumerable<ApiScope>> GetAllApiScopes()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var query =
                $"select * from {_options.DbSchema}.ApiScopes";
            var results = await connection.QueryAsync<ApiScopes>(query);
            var models = new List<ApiScope>();
            foreach(var item in results)
                models.Add(item.MapScope());

            return models;
        }
    }
}