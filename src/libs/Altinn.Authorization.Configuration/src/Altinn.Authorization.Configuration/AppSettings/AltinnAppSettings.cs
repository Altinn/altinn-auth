using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Configuration.AppSettings;

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
    /// Gets or sets the settings related to Azure App Configuration.
    /// </summary>
    public AppConfigurationSettings AppConfiguration { get; set; } = new AppConfigurationSettings();

    /// <summary>
    /// Gets or sets the settings related to Azure Entra ID (formerly Azure Active Directory).
    /// </summary>
    public EntraIdSettings EntraId { get; set; } = new EntraIdSettings();

    /// <summary>
    /// Gets or sets the settings for connecting to Azure Service Bus.
    /// </summary>
    public ServiceBusSettings ServiceBus { get; set; } = new ServiceBusSettings();

    /// <summary>
    /// Gets or sets the settings for connecting to a PostgreSQL database.
    /// </summary>
    public PostgresSettings Postgres { get; set; } = new PostgresSettings();

    /// <summary>
    /// Gets or sets the settings for Azure Application Insights.
    /// </summary>
    public ApplicationInsightsSettings ApplicationInsights { get; set; } = new ApplicationInsightsSettings();

    /// <summary>
    /// Gets or sets the Sentinel configuration value.
    /// This property may be used to define additional settings or identifiers relevant to the application.
    /// </summary>
    public string Sentinel { get; set; } = string.Empty;

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
