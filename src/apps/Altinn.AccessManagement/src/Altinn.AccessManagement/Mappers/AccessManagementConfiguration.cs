using AutoMapper;

namespace Altinn.AccessManagement.Mappers
{
    /// <summary>
    /// Configuration for automapper for access management
    /// </summary>
    public class AccessManagementConfiguration
    {
        /// <summary>
        /// access management mapping configuration
        /// </summary>
        public AccessManagementConfiguration(IMapperConfigurationExpression cfg)
        {
            cfg.AllowNullCollections = true;
        }
    }
}
