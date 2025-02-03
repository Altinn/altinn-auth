using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Cli.Database.Prompt;

/// <summary>
/// Extension methods for <see cref="SchemaItemPrompt"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public static class SchemaItemPromptExtensions
{
    /// <summary>
    /// Sets the <see cref="SchemaItemPrompt.Title">Title</see> of the <paramref name="prompt"/>.
    /// </summary>
    /// <param name="prompt">The prompt.</param>
    /// <param name="title">The new title.</param>
    /// <returns><paramref name="prompt"/>.</returns>
    public static SchemaItemPrompt Title(this SchemaItemPrompt prompt, string title)
    {
        prompt.Title = title;
        return prompt;
    }
}
