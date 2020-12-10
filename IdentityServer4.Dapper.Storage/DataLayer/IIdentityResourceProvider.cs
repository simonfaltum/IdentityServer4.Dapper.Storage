using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace IdentityServer4.Dapper.Storage.DataLayer
{
    public interface IIdentityResourceProvider
    {
        Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames);
        Task<IEnumerable<IdentityResource>> FindIdentityResourcesAllAsync();
        Task AddAsync(IdentityResource identityResource);
        Task UpdateAsync(IdentityResource identityResource);
        Task UpdateClaimsAsync(IdentityResource identityResource);
        Task<IdentityResource> FindIdentityResourcesByNameAsync(string name);

        Task RemoveAsync(string name);
    }
}
