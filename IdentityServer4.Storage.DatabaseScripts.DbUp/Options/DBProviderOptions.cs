namespace IdentityServer4.Storage.DatabaseScripts.DbUp.Options
{
    public class DBProviderOptions
    {
        public string DbSchema { get; set; } = "[dbo]";

        public string ConnectionString { get; set; }

    }
}
