namespace Altinn.Platform.Authorization.Configuration;

/// <summary>
/// Represents a set of configuration options needed for using the Altinn Platform Token Generator.
/// </summary>
public class TokenGeneratorSettings
{
    /// <summary>
    /// Gets or sets the url for the token generator.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets user for authorized access to the token generator.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Gets or sets password for authorized access to the token generator.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets the environment to use for the token generator.
    /// </summary>
    public string Env { get; set; }
}
