using System;
using IdentityServer4.Storage.DatabaseScripts.DbUp.Interfaces;
using IdentityServer4.Storage.DatabaseScripts.DbUp.Options;
using IdentityServer4.Storage.DatabaseScripts.DbUp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityServer4.Storage.DatabaseScripts.DbUp
{
    /// <summary>
    /// Extension methods to add Dapper support to IdentityServer.
    /// </summary>
    public static class IdentityServerDbScriptsExtensions
    {
        public static IIdentityServerBuilder AddDbUpDatabaseScripts(this IIdentityServerBuilder builder, Action<DBProviderOptions> dbProviderOptionsAction = null)
        {
            var options = GetDefaultOptions();
            dbProviderOptionsAction?.Invoke(options);
            builder.Services.AddSingleton(options);
            builder.Services.TryAddTransient<IIdentityServerConfigService, IdentityServerConfigService>();
            builder.Services.TryAddTransient<IIdentityServerMigrations, DefaultIdentityServerMigrations>();
            return builder;
        }
        public static DBProviderOptions GetDefaultOptions()
        {
            //config mssql
            var options = new DBProviderOptions();
            return options;
        }
    }
}
