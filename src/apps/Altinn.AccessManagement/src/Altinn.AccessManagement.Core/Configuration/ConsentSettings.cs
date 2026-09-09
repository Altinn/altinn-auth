namespace Altinn.AccessManagement.Core.Configuration
{
    public class ConsentSettings
    {
        /// <summary>
        /// The number of consent events to retrieve per page when querying the consent events
        /// </summary>
        public int EventsPageSize { get; set; } = 100;        
    }
}
