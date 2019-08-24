using IdentityServer4.Dapper.Storage.DataLayer;
using IdentityServer4.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Dapper.Storage.Services
{
    public class CorsPolicyService : ICorsPolicyService
    {
        private readonly IClientProvider _clientProvider;

        public CorsPolicyService(IClientProvider provider)
        {
            _clientProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task<bool> IsOriginAllowedAsync(string origin)
        {
            var distinctOrigins = await _clientProvider.QueryAllowedCorsOriginsAsync();

            var isAllowed = distinctOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);

            return isAllowed;
        }
    }
}
