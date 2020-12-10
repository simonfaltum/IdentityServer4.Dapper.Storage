using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Dapper.Storage.DataLayer;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Configuration;

namespace IdentityServer4.Dapper.Storage.Stores
{
    public class ResourceStore : IResourceStore
    {
        private readonly IApiResourceProvider _apiResource;
        private readonly IIdentityResourceProvider _identityResource;
        private readonly IApiScopesProvider _apiScopesProvider;
        private readonly IConfiguration _configuration;

        public ResourceStore(IConfiguration configuration, IApiResourceProvider apiResource, IIdentityResourceProvider identityResource, IApiScopesProvider apiScopesProvider)
        {
            _configuration = configuration;
            _apiResource = apiResource ?? throw new ArgumentNullException(nameof(apiResource));
            _identityResource = identityResource ?? throw new ArgumentNullException(nameof(identityResource));
            _apiScopesProvider = apiScopesProvider;
        }
        
        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
           
                var results = await _identityResource.FindIdentityResourcesByScopeAsync(scopeNames);
                if (results == null)
                    return new List<IdentityResource>();
                return results;
        }

        public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            var results = new List<ApiScope>();
            foreach (var name in scopeNames)
            {
                var identityResource = await _apiScopesProvider.FindApiScopeByNameAsync(name);
                results.Add(identityResource);
            }
            return results;
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            var api = await _apiResource.FindApiResourcesByScopeAsync(scopeNames);
            if (api == null)
                return new List<ApiResource>();
            return api;
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
        {
            var result = new List<ApiResource>();
            foreach (var item in apiResourceNames)
            {
                var api = await _apiResource.FindApiResourceAsync(item);
                result.Add(api);
            }
          
            return result;
        }

        public async Task<Resources> GetAllResourcesAsync()
        {
            Resources result;
            if (_configuration["ShowAllResources"] == "true")
            {
                var apis = await _apiResource.FindApiResourcesAllAsync();
                var identities = await _identityResource.FindIdentityResourcesAllAsync();
                var scopes = await _apiScopesProvider.GetAllApiScopes();
                result = new Resources(identities, apis,scopes);
            }
            else
            {
                result = new Resources();
            }

            return result;
        }
    }
}
