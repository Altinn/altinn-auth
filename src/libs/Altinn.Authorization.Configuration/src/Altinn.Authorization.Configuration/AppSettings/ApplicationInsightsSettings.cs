using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Configuration.AppSettings;

/// <summary>
/// Configuration settings for Azure Application Insights.
/// Used to configure the connection to the Application Insights service for telemetry and logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class ApplicationInsightsSettings
{
    /// <summary>
    /// The connection string for Azure Application Insights.
    /// This connection string is used to send telemetry data to the Application Insights instance.
    /// </summary>
    public string ConnectionString { get; set; }
}
