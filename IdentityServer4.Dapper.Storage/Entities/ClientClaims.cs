namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ClientClaims
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public int ClientId { get; set; }
    }
}
