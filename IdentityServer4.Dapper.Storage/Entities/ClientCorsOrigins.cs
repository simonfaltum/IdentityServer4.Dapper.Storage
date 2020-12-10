namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ClientCorsOrigins
    {
        public int Id { get; set; }
        public string Origin { get; set; }
        public int ClientId { get; set; }
    }
}
