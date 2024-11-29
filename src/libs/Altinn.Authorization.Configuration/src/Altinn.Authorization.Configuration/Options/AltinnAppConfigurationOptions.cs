namespace Altinn.Authorization.Configuration.Options;

/// <summary>
/// Provides customization of Azure App Configuration integration.
/// This class allows the specification of labels to filter settings, both for 
/// feature flags and keys, to customize the retrieval of configurations.
/// </summary>
public class AltinnAppConfigurationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnAppConfigurationOptions"/> class 
    /// and applies additional configuration through the provided delegate.
    /// </summary>
    /// <param name="configureOptions">A delegate function for configuring integration options. </param>
    public AltinnAppConfigurationOptions(Action<AltinnAppConfigurationOptions> configureOptions) => configureOptions?.Invoke(this);

    /// <summary>
    /// Gets the default label
    /// </summary>
    public const string DefaultLabel = "default";

    /// <summary>
    /// A collection of feature-specific labels used to filter configuration settings.
    /// </summary>
    internal HashSet<string> FeatureLabels { get; } = [];

    /// <summary>
    /// A collection of key-specific labels used to filter configuration settings.
    /// </summary>
    internal HashSet<string> KeyLabels { get; } = [];

    /// <summary>
    /// Adds one or more key-specific labels to the configuration options. 
    /// These labels are used to filter and retrieve settings from Azure App Configuration 
    /// based on the specified feature flags.
    /// </summary>
    /// <param name="labels">An array of strings representing the key labels to be added.</param>
    /// <returns>The current instance of <see cref="AltinnAppConfigurationOptions"/> for method chaining.</returns>
    public AltinnAppConfigurationOptions AddKeyLabels(params string[] labels)
    {
        AddLabels(KeyLabels, labels);
        return this;
    }

    /// <summary>
    /// Adds one or more feature-specific labels to the configuration options. 
    /// These labels are used to filter and retrieve settings from Azure App Configuration 
    /// based on the specified feature flags.
    /// </summary>
    /// <param name="labels">An array of strings representing the feature labels to be added.</param>
    /// <returns>The current instance of <see cref="AltinnAppConfigurationOptions"/> for method chaining.</returns>
    public AltinnAppConfigurationOptions AddFeatureLabels(params string[] labels)
    {
        AddLabels(FeatureLabels, labels);
        return this;
    }

    /// <summary>
    /// Adds the default label for reading configuration settings.
    /// This method allows for easy addition of the "default" label.
    /// </summary>
    /// <returns>The current instance of <see cref="AltinnAppConfigurationOptions"/> for method chaining.</returns>
    public AltinnAppConfigurationOptions AddDefaults()
    {
        AddKeyLabels(DefaultLabel);
        AddFeatureLabels(DefaultLabel);
        return this;
    }

    /// <summary>
    /// Adds the specified labels to the given set.
    /// </summary>
    /// <param name="set">The set to which the labels will be added.</param>
    /// <param name="labels">An array of strings representing the labels to be added.</param>
    private void AddLabels(HashSet<string> set, string[] labels)
    {
        foreach (var label in labels)
        {
            set.Add(label);
        }
    }
}
