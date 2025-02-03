using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// Provides default value for an option from an environment variable.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EnvironmentVariableAttribute
    : ParameterValueProviderAttribute
{
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableAttribute"/> class.
    /// </summary>
    /// <param name="name">The environment variable name.</param>
    public EnvironmentVariableAttribute(string name)
    {
        _name = name;
    }

    /// <inheritdoc/>
    public override bool TryGetValue(CommandParameterContext context, out object? result)
    {
        result = null;

        if (context.Value is null)
        {
            var valStr = Environment.GetEnvironmentVariable(_name);

            if (valStr is null)
            {
                return false;
            }

            result = valStr;
            return true;
        }

        return false;
    }
}
