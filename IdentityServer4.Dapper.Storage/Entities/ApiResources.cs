using System;

namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ApiResources
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Updated { get; set; }
        public DateTime? LastAccessed { get; set; }
        public bool NonEditable { get; set; }
        public bool ShowInDiscoveryDocument { get; set; }
        public string AllowedAccessTokenSigningAlgorithms { get; set; }
    }
}
