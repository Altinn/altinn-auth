namespace Altinn.AccessManagement.Persistence.Configuration
{
    /// <summary>
    /// Settings for Postgres database
    /// </summary>
    public class PostgreSQLSettings
    {
        /// <summary>
        /// Connection string for the postgres db
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Password for app user for the postgres db
        /// </summary>
        public string AuthorizationDbPwd { get; set; }

        /// <summary>
        /// The number of seconds to wait before considering a consent event as final. This setting is used to introduce a safety lag when processing consent events, ensuring that any recent changes are fully propagated and reducing the risk of inconsistencies. Adjusting this value can help balance the trade-off between data freshness and consistency.
        /// </summary>
        public int ConsentEventsSafetyLag { get; set; } = 300;
    }
}
