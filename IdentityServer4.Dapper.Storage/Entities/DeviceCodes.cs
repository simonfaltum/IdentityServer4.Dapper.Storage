using System;

namespace IdentityServer4.Dapper.Storage.Entities
{
    public class DeviceCodes
    {
        public string DeviceCode { get; set; }
        public string UserCode { get; set; }
        public string SubjectId { get; set; }
        public string ClientId { get; set; }
        public DateTime CreationTime { get; set; } = DateTime.Now;
        public DateTime Expiration { get; set; }
        public string Data { get; set; }
    }
}
