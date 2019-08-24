using System;

namespace IdentityServer4.Dapper.Storage.Entities
{
    public class PersistedGrants
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public string SubjectId { get; set; }
        public string ClientId { get; set; }
        public DateTime CreationTime { get; set; } = DateTime.Now;
        public DateTime? Expiration { get; set; }
        public string Data { get; set; }
    }
}
