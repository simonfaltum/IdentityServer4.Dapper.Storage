namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ApiResourceScopes
    {
        public int Id { get; set; }
        public int ApiResourceId { get; set; }
        public string Scope { get; set; }
    }
}