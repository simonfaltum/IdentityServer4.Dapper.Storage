using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace IdentityServer4.Dapper.Storage.DataLayer
{
    public interface IPersistedGrantProvider
    {
        Task<IEnumerable<PersistedGrant>> GetAll(string subjectId);
        Task<IEnumerable<PersistedGrant>> GetAll(string subjectId, string clientId);
        Task<IEnumerable<PersistedGrant>> GetAll(string subjectId, string clientId, string type);
        Task<PersistedGrant> Get(string key);
        Task Add(PersistedGrant token);
        Task Update(PersistedGrant token);
        Task RemoveAll(string subjectId, string clientId);
        Task RemoveAll(string subjectId, string clientId, string type);
        Task Remove(string key);

        Task<int> QueryExpired(DateTimeOffset dateTime);
        Task RemoveRange(DateTimeOffset dateTime);
    }
}
