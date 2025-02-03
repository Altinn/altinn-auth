using System;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// Character buffer helpers.
/// </summary>
[ExcludeFromCodeCoverage]
public static class CharBuffers
{
    private const int KIBIBYTE_CHAR_COUNT = 1024 / sizeof(char);
    private const int MIBIBYTE_CHAR_COUNT = 1024 * KIBIBYTE_CHAR_COUNT;

    /// <summary>
    /// Creates a char buffer of <paramref name="count"/> MiB.
    /// </summary>
    /// <param name="count">The number of MiB the resulting buffer should be.</param>
    /// <returns>A char buffer.</returns>
    public static Memory<char> MiB(int count) => new char[MIBIBYTE_CHAR_COUNT * count];
}
