namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ClientPostLogoutRedirectUris
    {
        public int Id { get; set; }
        public string PostLogoutRedirectUri { get; set; }
        public int ClientId { get; set; }
    }
}
