namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ClientRedirectUris
    {
        public int Id { get; set; }
        public string RedirectUri { get; set; }
        public int ClientId { get; set; }


    }
}
