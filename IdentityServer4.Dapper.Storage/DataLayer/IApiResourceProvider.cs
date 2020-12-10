using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace IdentityServer4.Dapper.Storage.DataLayer
{
    public interface IApiResourceProvider
    {
        Task<ApiResource> FindApiResourceAsync(string name);

        Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames);

        Task<IEnumerable<ApiResource>> FindApiResourcesAllAsync();

        Task AddAsync(ApiResource apiResource);

        Task UpdateAsync(ApiResource apiResource);

        Task UpdateApiResourceSecretsByApiResourceIdAsync(ApiResource apiResource);

        Task UpdateScopesByApiResourceIdAsync(ApiResource apiResource);

        Task UpdateClaimsByApiResourceIdAsync(ApiResource apiResource);

        Task RemoveAsync(string name);
    }
}
