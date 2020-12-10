namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ClientIdPRestrictions
    {
        public int Id { get; set; }
        public string Provider { get; set; }
        public int ClientId { get; set; }
    }
}
