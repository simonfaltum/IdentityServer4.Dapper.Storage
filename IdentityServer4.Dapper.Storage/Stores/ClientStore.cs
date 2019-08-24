using IdentityServer4.Dapper.Storage.DataLayer;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using System;
using System.Threading.Tasks;

namespace IdentityServer4.Dapper.Storage.Stores
{
    public class ClientStore : IClientStore
    {
        private readonly IClientProvider _clientDB;

        public ClientStore(IClientProvider client)
        {
            _clientDB = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            var client = await _clientDB.FindClientByIdAsync(clientId);

            return client;
        }
    }
}
