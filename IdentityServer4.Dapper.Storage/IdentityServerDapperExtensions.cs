﻿using System;
using System.Linq;
using IdentityServer4.Dapper.Storage.DataLayer;
using IdentityServer4.Dapper.Storage.Options;
using IdentityServer4.Dapper.Storage.Services;
using IdentityServer4.Dapper.Storage.Stores;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IdentityServer4.Dapper.Storage
{
    /// <summary>
    /// Extension methods to add Dapper support to IdentityServer.
    /// </summary>
    public static class IdentityServerDapperExtensions
    {
        public static IIdentityServerBuilder AddSQLConnection(this IIdentityServerBuilder builder, Action<DBProviderOptions> dbProviderOptionsAction = null)
        {
            var options = GetDefaultOptions();
            dbProviderOptionsAction?.Invoke(options);
            builder.Services.AddSingleton(options);
            return builder;
        }
        public static DBProviderOptions GetDefaultOptions()
        {
            //config mssql
            var options = new DBProviderOptions
            {
                GetLastInsertID = "select @@IDENTITY;"
            };

            return options;
        }

        /// <summary>
        /// Configures Dapper implementation of IClientStore, IResourceStore, and ICorsPolicyService with IdentityServer.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="storeOptionsAction"></param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddDapperConfigurationStore(this IIdentityServerBuilder builder, Action<ConfigurationStoreOptions> storeOptionsAction = null)
        {
            var options = new ConfigurationStoreOptions();
            storeOptionsAction?.Invoke(options);
            builder.Services.AddSingleton(options);

            builder.Services.AddSingleton<IClientProvider, DefaultClientProvider>();
            builder.Services.AddSingleton<IApiResourceProvider, DefaultApiResourceProvider>();
            builder.Services.AddSingleton<IIdentityResourceProvider, DefaultIdentityResourceProvider>();
            builder.Services.AddSingleton<IApiScopesProvider, DefaultApiScopesProvider>();
            builder.AddClientStore<ClientStore>();
            builder.AddResourceStore<ResourceStore>();
            builder.AddCorsPolicyService<CorsPolicyService>();
            return builder;
        }

        /// <summary>
        /// Configures Dapper implementation of IPersistedGrantStore with IdentityServer.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="storeOptionsAction"></param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddDapperOperationalStore(this IIdentityServerBuilder builder, Action<OperationalStoreOptions> storeOptionsAction = null)
        {
            builder.Services.AddSingleton<TokenCleanup>();
            builder.Services.AddSingleton<IHostedService, TokenCleanupHost>();//auto clear expired tokens

            builder.Services.AddSingleton<IPersistedGrantProvider, DefaultPersistedGrantProvider>();

            var storeOptions = new OperationalStoreOptions();
            storeOptionsAction?.Invoke(storeOptions);
            builder.Services.AddSingleton(storeOptions);

            var memopersistedstore = builder.Services.FirstOrDefault(c => c.ServiceType == typeof(IPersistedGrantStore));
            if (memopersistedstore != null)
            {
                builder.Services.Remove(memopersistedstore);
            }
            builder.Services.AddSingleton<IPersistedGrantStore, PersistedGrantStore>();
            memopersistedstore = builder.Services.FirstOrDefault(c => c.ServiceType == typeof(IPersistedGrantStore));
            return builder;
        }

        /// <summary>
        /// Sets up IClientProvider, IApiResourceProvider and IIdentityResourceProvider interfaces. Useful if you need to read/write to persistence from outside IdentityServer, i.e. adding new clients.
        /// This should only be used if IdentityServer is not used/added to the project directly.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="dbProviderOptionsAction">Make sure to minimum set connectionstring here.</param>
        /// <returns></returns>
        public static IServiceCollection AddIdentityServerDapperDbProviders(this IServiceCollection services, Action<DBProviderOptions> dbProviderOptionsAction = null)
        {
            var options = GetDefaultOptions();
            dbProviderOptionsAction?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<IClientProvider, DefaultClientProvider>();
            services.AddSingleton<IApiResourceProvider, DefaultApiResourceProvider>();
            services.AddSingleton<IIdentityResourceProvider, DefaultIdentityResourceProvider>();
            return services;
        }
    }
}
