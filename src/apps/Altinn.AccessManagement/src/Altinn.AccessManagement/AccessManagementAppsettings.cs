using Altinn.Authorization.Host.Appsettings;
using Altinn.Authorization.Host.Lease.Appsettings;
using Altinn.Authorization.Integration.Platform.Appsettings;

namespace Altinn.AccessManagement;

/// <summary>
/// Represents application settings for Access Management, mapping configurations from appsettings files.
/// </summary>
public class AccessManagementAppsettings
{
    /// <summary>
    /// Initializes a new instance of <see cref="AccessManagementAppsettings"/>.
    /// </summary>
    public AccessManagementAppsettings() { }

    /// <summary>
    /// Initializes a new instance of <see cref="AccessManagementAppsettings"/> and binds configuration values.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public AccessManagementAppsettings(IConfiguration configuration)
    {
        configuration.Bind(this);
    }

    /// <summary>
    /// Gets or sets the application configuration settings.
    /// </summary>
    public AppConfigurationSettings AppConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the application configuration settings.
    /// </summary>
    public PlatformSettings Platform { get; set; }

    /// <summary>
    /// Gets or sets the lease settings.
    /// </summary>
    public LeaseSettings Lease { get; set; }

    /// <summary>
    /// Gets or sets the current environment (e.g., at22, at23, tt02, prod).
    /// </summary>
    public string Environment { get; set; }
}
