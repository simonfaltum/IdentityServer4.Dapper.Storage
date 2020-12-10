using System;
using System.Linq;
using IdentityServer4.Dapper.Storage.DataLayer;
using IdentityServer4.Dapper.Storage.Options;
using IdentityServer4.Storage.DatabaseScripts.DbUp.Interfaces;
using IdentityServer4.Storage.DatabaseScripts.DbUp.Seeding;

namespace IdentityServer4.Storage.DatabaseScripts.DbUp.Services
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
        
        public IdentityServerConfigService(string connectionString, string schema)
        {
            var dbSchema = schema.IndexOf("[", StringComparison.InvariantCultureIgnoreCase) >= 0 ? schema : $"[{schema}]";
            this.clientProvider = new DefaultClientProvider(new DBProviderOptions(){ ConnectionString = connectionString, DbSchema = dbSchema }, null);
            this.identityResourceProvider = new DefaultIdentityResourceProvider(new DBProviderOptions(){ ConnectionString = connectionString, DbSchema = dbSchema }, null);
            this.apiResourceProvider = new DefaultApiResourceProvider(new DBProviderOptions(){ ConnectionString = connectionString, DbSchema = dbSchema }, null);
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
