using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Host.AppSettings;

/// <summary>
/// Represents the application settings for the Altinn authorization.
/// This class encapsulates configuration settings required for different default applications,
/// such as settings for Azure App Configuration, Entra ID, Service Bus, PostgreSQL,
/// Application Insights, and other relevant configurations.
/// </summary>
public class AltinnAppSettings : IValidateOptions<AltinnAppSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnAppSettings"/> class.
    /// This constructor is typically used when instantiating the class without any configuration.
    /// </summary>
    public AltinnAppSettings()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnAppSettings"/> class
    /// and binds the provided <paramref name="configuration"/> to the properties of this instance.
    /// </summary>
    /// <param name="configuration">The configuration instance from which to bind settings.</param>
    public AltinnAppSettings(IConfiguration configuration)
    {
        configuration.Bind(this);
    }

    /// <summary>
    /// Azure Client
    /// </summary>
    public AzureSettings Entra { get; set; }

    /// <summary>
    /// Gets or sets the settings related to Azure App Configuration.
    /// </summary>
    public AppConfigurationSettings AppConfiguration { get; set; } = new AppConfigurationSettings();

    /// <summary>
    /// Gets or sets the settings for connecting to Azure Service Bus.
    /// </summary>
    public ServiceBusSettings ServiceBus { get; set; } = new ServiceBusSettings();

    /// <summary>
    /// Gets or sets the settings for Azure Application Insights.
    /// </summary>
    public ApplicationInsightsSettings ApplicationInsights { get; set; } = new ApplicationInsightsSettings();

    /// <summary>
    /// Gets or sets the settings for connecting to a PostgreSQL database.
    /// </summary>
    public PostgresSettings Postgres { get; set; } = new PostgresSettings();

    /// <summary>
    /// AzureSettings
    /// </summary>
    public class AzureSettings
    {
        /// <summary>
        /// Workload Identity
        /// </summary>
        public string ClientID { get; set; }
    }

    /// <summary>
    /// ApplicationInsightsSettings
    /// </summary>
    public class ApplicationInsightsSettings
    {
        /// <summary>
        /// ConnectionString
        /// </summary>
        public string ConnectionString { get; set; }
    }

    /// <summary>
    /// Configuration settings for Azure Service Bus.
    /// This class holds the settings required to connect to the Azure Service Bus service.
    /// </summary>
    public class ServiceBusSettings
    {
        /// <summary>
        /// The URI of the Azure Service Bus endpoint.
        /// This endpoint is used to send and receive messages from the Azure Service Bus service.
        /// </summary>
        public Uri Endpoint { get; set; }
    }

    /// <summary>
    /// AppConfigurationSettings
    /// </summary>
    public class AppConfigurationSettings
    {
        /// <summary>
        /// The URI of the Azure App Configuration service endpoint.
        /// This endpoint is used to retrieve configuration data from Azure App Configuration.
        /// </summary>
        public Uri Endpoint { get; set; }
    }

    /// <summary>
    /// PostgresSettings
    /// </summary>
    public class PostgresSettings
    {
        /// <summary>
        /// AppConnectionString
        /// </summary>
        public string AppConnectionString { get; set; }

        /// <summary>
        /// MigrationConnectionString
        /// </summary>
        public string MigrationConnectionString { get; set; }
    }

    /// <summary>
    /// Validates the options for the <see cref="AltinnAppSettings"/> class.
    /// This method is intended to ensure that the settings provided are correct and complete.
    /// </summary>
    /// <param name="name">The name of the options being validated, if applicable.</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/> indicating whether the validation succeeded or failed.</returns>
    public ValidateOptionsResult Validate(string name, AltinnAppSettings options)
    {
        return new ValidateOptionsResult();
    }
}
