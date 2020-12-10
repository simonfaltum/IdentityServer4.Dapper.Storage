using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace IdentityServer4.Dapper.Storage.DataLayer
{
    public interface IClientProvider
    {
        Task<Client> FindClientByIdAsync(string clientId);
        Task AddAsync(Client client);

        Task<IEnumerable<string>> QueryAllowedCorsOriginsAsync();

        Task RemoveAsync(string clientId);

        Task UpdateAsync(Client client);

        Task UpdateGrantTypesAsync(Client client);
        Task UpdateRedirectUrisAsync(Client client);
        Task UpdatePostLogoutRedirectUrisAsync(Client client);
        Task UpdateScopesAsync(Client client);
        Task UpdateSecretsAsync(Client client);
        Task UpdateClaimsAsync(Client client);
        Task UpdateIdPRestrictionsAsync(Client client);
        Task UpdateCorsOriginsAsync(Client client);
        Task UpdatePropertiesAsync(Client client);
    }
}
