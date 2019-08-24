namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ClientGrantTypes
    {
        public int Id { get; set; }
        public string GrantType { get; set; }
        public int ClientId { get; set; }


    }
}
