namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ClientScopes
    {
        public int Id { get; set; }
        public string Scope { get; set; }
        public int ClientId { get; set; }


    }
}
