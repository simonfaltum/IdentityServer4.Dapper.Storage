namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ApiScopeClaims
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int ApiScopeId { get; set; }
    }
}
