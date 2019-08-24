using IdentityServer4.Dapper.Storage.DataLayer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IdentityServer4.Storage.DatabaseScripts.DbUp
{
    public class IdentityServerConfigService : IIdentityServerConfigService
    {

        private readonly IIdentityResourceProvider identityResourceProvider;
        private readonly IApiResourceProvider apiResourceProvider;
        private readonly IClientProvider clientProvider;

        public IdentityServerConfigService(IClientProvider clientProvider, IIdentityResourceProvider identityResourceProvider, IApiResourceProvider apiResourceProvider)
        {
            this.clientProvider = clientProvider;
            this.identityResourceProvider = identityResourceProvider;
            this.apiResourceProvider = apiResourceProvider;
        }
        public void SetupIdentityDefaultConfig()
        {

            Console.WriteLine("Seeding database...");
            EnsureSeedClientData();
            EnsureSeedIdentityResourcesData();
            EnsureSeedApiResourcesData();
            Console.WriteLine("Done seeding database.");
            Console.WriteLine();

        }
        private void EnsureSeedClientData()
        {
            if (clientProvider != null)
            {
                Console.WriteLine("Clients being populated");
                foreach (var client in IdentityServerConfig.GetClients().ToList())
                {
                    if (clientProvider.FindClientByIdAsync(client.ClientId).Result == null)
                    {
                        clientProvider.AddAsync(client);
                    }
                }
            }
            else
            {
                Console.WriteLine("Clients already populated");
            }
        }
        private void EnsureSeedIdentityResourcesData()
        {
            if (identityResourceProvider != null)
            {
                Console.WriteLine("IdentityResources being populated");
                foreach (var resource in IdentityServerConfig.GetIdentityResources().ToList())
                {
                    if (identityResourceProvider.FindIdentityResourcesByNameAsync(resource.Name).Result == null)
                    {
                        identityResourceProvider.AddAsync(resource);
                    }
                }
            }
            else
            {
                Console.WriteLine("IdentityResources already populated");
            }
        }
        private void EnsureSeedApiResourcesData()
        {
            if (apiResourceProvider != null)
            {
                Console.WriteLine("ApiResources being populated");
                foreach (var resource in IdentityServerConfig.GetApis().ToList())
                {
                    if (apiResourceProvider.FindApiResourceAsync(resource.Name).Result == null)
                    {
                        apiResourceProvider.AddAsync(resource);
                    }
                }
            }
            else
            {
                Console.WriteLine("ApiResources already populated");
            }
        }
    }
}
