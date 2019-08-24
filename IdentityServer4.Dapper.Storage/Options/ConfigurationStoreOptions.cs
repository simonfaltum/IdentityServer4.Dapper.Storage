using IdentityServer4.Dapper.Storage.DataLayer;

namespace IdentityServer4.Dapper.Storage.Options
{
    public class ConfigurationStoreOptions
    {
        public IApiResourceProvider ApiResourceProvider { get; set; }


        public IIdentityResourceProvider IdentityResourceProvider { get; set; }


        public IClientProvider ClientResourceProvider { get; set; }
    }
}