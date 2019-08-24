namespace IdentityServer4.Storage.DatabaseScripts.DbUp
{
    public interface IIdentityServerMigrations
    {
        bool UpgradeDatabase(bool withSeed = false);
    }
}