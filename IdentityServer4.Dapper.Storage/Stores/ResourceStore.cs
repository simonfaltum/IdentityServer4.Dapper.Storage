using IdentityServer4.Dapper.Storage.DataLayer;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer4.Dapper.Storage.Stores
{
    public class ResourceStore : IResourceStore
    {
        private readonly IApiResourceProvider _apiResource;
        private readonly IIdentityResourceProvider _identityResource;
        private readonly IConfiguration _configuration;

        public ResourceStore(IConfiguration configuration, IApiResourceProvider apiResource, IIdentityResourceProvider identityResource)
        {
            this._configuration = configuration;
            this._apiResource = apiResource ?? throw new ArgumentNullException(nameof(apiResource));
            this._identityResource = identityResource ?? throw new ArgumentNullException(nameof(identityResource));
        }

        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            var api = _apiResource.FindApiResourceAsync(name);

            return await api;
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var models = _apiResource.FindApiResourcesByScopeAsync(scopeNames);
            return await (models);
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var identityResources = _identityResource.FindIdentityResourcesByScopeAsync(scopeNames);//;
            var results = await identityResources;

            return (results);
        }


        public async Task<Resources> GetAllResourcesAsync()
        {
            Resources result;
            if (_configuration["ShowAllResources"] == "true")
            {
                var apis = await _apiResource.FindApiResourcesAllAsync();
                var identities = await _identityResource.FindIdentityResourcesAllAsync();
                result = new Resources(identities, apis);

            }
            else
            {
                result = new Resources();
            }

            return result;
        }
    }
}
