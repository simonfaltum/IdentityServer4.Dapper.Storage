using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Dapper.Storage.Entities;
using IdentityServer4.Models;

namespace IdentityServer4.Dapper.Storage.DataLayer
{
    public interface IApiScopesProvider
    {
        public Task<ApiScope> FindApiScopeByNameAsync(string name);
        Task RemoveApiScopes(IEnumerable<ApiScopes> apiScopes);
        Task InsertNewApiScope(ApiScope apiScope);
        Task<IEnumerable<ApiScope>> GetAllApiScopes();
    }
}