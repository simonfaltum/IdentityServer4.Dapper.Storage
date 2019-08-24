using System;

namespace IdentityServer4.Dapper.Storage.Entities
{
    public class ApiSecrets
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
        public DateTime? Expiration { get; set; }
        public string Type { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public int ApiResourceId { get; set; }

    }
}
