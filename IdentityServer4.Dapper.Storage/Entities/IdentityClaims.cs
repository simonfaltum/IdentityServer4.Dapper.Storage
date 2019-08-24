namespace IdentityServer4.Dapper.Storage.Entities
{
    public class IdentityClaims
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int IdentityResourceId { get; set; }

    }
}
