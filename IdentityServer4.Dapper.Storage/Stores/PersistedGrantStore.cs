using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Dapper.Storage.DataLayer;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace IdentityServer4.Dapper.Storage.Stores
{
    /// <summary>
    /// Implementation of IPersistedGrantStore that uses Dapper.
    /// </summary>
    /// <seealso cref="IdentityServer4.Stores.IPersistedGrantStore" />
    public class PersistedGrantStore : IPersistedGrantStore
    {
        private readonly IPersistedGrantProvider _persistedGrantProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedGrantStore"/> class.
        /// </summary>
        /// <param name="persistedGrantProvider">the provider.</param>
        public PersistedGrantStore(IPersistedGrantProvider persistedGrantProvider)
        {
            _persistedGrantProvider = persistedGrantProvider ?? throw new ArgumentNullException(nameof(persistedGrantProvider));
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var results = await _persistedGrantProvider.GetAll(subjectId);

            return results;
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var result = await _persistedGrantProvider.Get(key);
            return result;
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            var results = await _persistedGrantProvider.GetAll(filter.SubjectId);

            return results;
        }


   
        public async Task RemoveAsync(string key)
        {
            var persistedGrant = await _persistedGrantProvider.Get(key);
            if (persistedGrant != null)
            {
                await _persistedGrantProvider.Remove(key);
            }
        }

        public async Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            //throw new NotImplementedException();
            await _persistedGrantProvider.RemoveAll(filter.SubjectId, filter.ClientId, filter.Type);
        }

        public async Task StoreAsync(PersistedGrant token)
        {
            var existing = await _persistedGrantProvider.Get(token.Key);

            if (existing == null)
            {
                await _persistedGrantProvider.Add(token);
            }
            else
            {
                await _persistedGrantProvider.Update(token);
            }
        }
    }
}