#nullable enable

using Altinn.ResourceRegistry.Core.Models.Versioned;
using FluentAssertions.Formatting;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Altinn.ResourceRegistry.Tests.Utils;

internal class VersionedEntityConditionResultFormatter 
    : IValueFormatter
{
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "This is the recommended way to add formatters to FluentAssertions")]
    internal static void Register()
    {
        Formatter.AddFormatter(Instance);
    }

    private static IValueFormatter Instance { get; } = new VersionedEntityConditionResultFormatter();

    private VersionedEntityConditionResultFormatter()
    {
    }

    public bool CanHandle(object value)
        => value is VersionedEntityConditionResult;

    public void Format(object value, FormattedObjectGraph formattedGraph, FormattingContext context, FormatChild formatChild)
    {
        formattedGraph.AddFragment(((VersionedEntityConditionResult)value).ToString());
    }
}
