namespace IdentityServer4.Dapper.Storage.Entities
{
    public class IdentityProperties
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public int IdentityResourceId { get; set; }
    }
}
