namespace IdentityServer4.Dapper.Storage.Options
{
    public class DBProviderOptions
    {
        public string DbSchema { get; set; } = "[dbo]";

        /// <summary>
        /// connection string for dapper
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// time out setting
        /// </summary>
        public int CommandTimeOut { get; set; } = 3000;

        /// <summary>
        ///  sql for get new id inserted
        /// </summary>
        public string GetLastInsertID { get; set; }



    }
}
