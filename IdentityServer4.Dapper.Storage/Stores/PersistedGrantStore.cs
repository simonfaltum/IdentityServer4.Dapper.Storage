using IdentityServer4.Dapper.Storage.DataLayer;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer4.Dapper.Storage.Stores
{
    /// <summary>
    /// Implementation of IPersistedGrantStore thats uses Dapper.
    /// </summary>
    /// <seealso cref="IdentityServer4.Stores.IPersistedGrantStore" />
    public class PersistedGrantStore : IPersistedGrantStore
    {
        private readonly IPersistedGrantProvider _persistedgrantprovider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedGrantStore"/> class.
        /// </summary>
        /// <param name="persistedGrantProvider">the provider.</param>
        /// <param name="logger">the logger.</param>
        public PersistedGrantStore(IPersistedGrantProvider persistedGrantProvider, ILogger<ClientStore> logger)
        {
            _persistedgrantprovider =
                persistedGrantProvider ?? throw new ArgumentNullException(nameof(persistedGrantProvider));
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var results = await _persistedgrantprovider.GetAll(subjectId);

            return results;
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var result = await _persistedgrantprovider.Get(key);
            return result;
        }

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            var persistedGrants = await _persistedgrantprovider.GetAll(subjectId, clientId);


            await _persistedgrantprovider.RemoveAll(subjectId, clientId);
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            var persistedGrants = await _persistedgrantprovider.GetAll(subjectId, clientId, type);


            await _persistedgrantprovider.RemoveAll(subjectId, clientId, type);
        }

        public async Task RemoveAsync(string key)
        {
            var persistedGrant = await _persistedgrantprovider.Get(key);
            if (persistedGrant != null)
            {
                await _persistedgrantprovider.Remove(key);
            }
        }

        public async Task StoreAsync(PersistedGrant token)
        {
            var existing = await _persistedgrantprovider.Get(token.Key);

            if (existing == null)
            {
                await _persistedgrantprovider.Add(token);
            }
            else
            {
                await _persistedgrantprovider.Update(token);
            }
        }
    }
}