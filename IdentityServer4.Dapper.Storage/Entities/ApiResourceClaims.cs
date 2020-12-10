namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ApiResourceClaims
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int ApiResourceId { get; set; }
    }
}
