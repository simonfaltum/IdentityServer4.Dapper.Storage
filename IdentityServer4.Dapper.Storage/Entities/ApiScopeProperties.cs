namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ApiScopeProperties
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public int ApiScopeId { get; set; }
    }
}