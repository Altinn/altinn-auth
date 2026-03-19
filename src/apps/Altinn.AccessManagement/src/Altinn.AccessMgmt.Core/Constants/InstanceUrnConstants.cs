using System;
using System.Collections.Generic;

namespace Altinn.AccessMgmt.Core.Constants;

/// <summary>
/// Constants for valid instance URN formats used in delegation.
/// These formats are used temporarily until full integration with Dialogporten's dialog-lookup API.
/// </summary>
public static class InstanceUrnConstants
{
    /// <summary>
    /// URN prefix for Altinn Apps instance identifiers.
    /// Format: urn:altinn:instance-id:{id}
    /// </summary>
    public const string AltinnAppsPrefix = "urn:altinn:instance-id:";

    /// <summary>
    /// URN prefix for Correspondence instance identifiers.
    /// Format: urn:altinn:correspondence-id:{id}
    /// </summary>
    public const string CorrespondencePrefix = "urn:altinn:correspondence-id:";

    /// <summary>
/// URN prefix for Dialog instance identifiers.
/// Format: urn:altinn:dialog-id:{id}
/// </summary>
    public const string DialogPrefix = "urn:altinn:dialog-id:";

    /// <summary>
    /// Gets all valid URN prefixes for instance identifiers.
    /// </summary>
    private static readonly string[] ValidPrefixesArray =
    [
        AltinnAppsPrefix,
        CorrespondencePrefix,
        DialogPrefix
    ];

    public static IReadOnlyList<string> ValidPrefixes { get; } = Array.AsReadOnly(ValidPrefixesArray);
}
