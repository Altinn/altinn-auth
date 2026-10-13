using System.Text.RegularExpressions;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Utils.Mappers;

/// <summary>
/// Pure-logic unit tests for the activity log entry text: the sentence
/// <see cref="DtoMapper.Convert(ActivityLog, string)"/> builds from the catalog template
/// and the entry's name snapshots, and the catalog templates themselves.
/// </summary>
[UnitTest]
public partial class DtoMapperActivityLogTest
{
    private static readonly string[] KnownPlaceholders = ["from", "to", "via", "by", "role", "package", "resource"];

    private static readonly string[] Languages = ["nob", "nno", "eng"];

    private static ActivityLog MakeEntry(ActivityLogType type, ActivityLogSubtype? subtype, ActivityLogTrigger trigger, RequestStatusV2? status = null) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Subtype = subtype,
        Trigger = trigger,
        Status = status,
        When = DateTimeOffset.UtcNow,
        FromName = "ACME AS",
        ToName = "OLA NORMANN",
        ViaName = "REGNSKAP AS",
        ByName = "KARI JOHNSEN",
        RoleName = "Rettighetshaver",
        PackageName = "Toll",
        ResourceName = "Tollskjema",
        ItemId = Guid.NewGuid(),
    };

    // ── Convert(ActivityLog) — Text ────────────────────────────────────────────
    [Theory]
    [InlineData(null, "OLA NORMANN fikk enkelttjenesten «Tollskjema» hos ACME AS tildelt av KARI JOHNSEN")]
    [InlineData("nno", "OLA NORMANN fekk enkelttenesta «Tollskjema» hos ACME AS tildelt av KARI JOHNSEN")]
    [InlineData("eng", "OLA NORMANN was granted the single service “Tollskjema” at ACME AS by KARI JOHNSEN")]
    public void Convert_RendersTextInRequestedLanguage(string? languageCode, string expected)
    {
        var entry = MakeEntry(ActivityLogType.Assignment, ActivityLogSubtype.Resource, ActivityLogTrigger.Created);

        var dto = DtoMapper.Convert(entry, languageCode);

        dto.Text.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "OLA NORMANN fikk enkelttjenesten «Tollskjema» hos ACME AS tildelt av ukjent")]
    [InlineData("nno", "OLA NORMANN fekk enkelttenesta «Tollskjema» hos ACME AS tildelt av ukjend")]
    [InlineData("eng", "OLA NORMANN was granted the single service “Tollskjema” at ACME AS by unknown")]
    public void Convert_MissingName_UsesNeutralWord(string? languageCode, string expected)
    {
        var entry = MakeEntry(ActivityLogType.Assignment, ActivityLogSubtype.Resource, ActivityLogTrigger.Created);
        entry.ByName = null;

        var dto = DtoMapper.Convert(entry, languageCode);

        dto.Text.Should().Be(expected);
    }

    [Fact]
    public void Convert_StatusSpecificTemplateBeatsFallback()
    {
        var entry = MakeEntry(ActivityLogType.Request, ActivityLogSubtype.Package, ActivityLogTrigger.Updated, RequestStatusV2.Approved);

        var dto = DtoMapper.Convert(entry);

        dto.ActivityTypeId.Should().Be(ActivityTypeConstants.RequestPackageUpdatedApproved.Id);
        dto.Text.Should().Be("KARI JOHNSEN godkjente forespørselen om tilgangspakken «Toll» for OLA NORMANN hos ACME AS");
    }

    [Fact]
    public void Convert_NameLookingLikePlaceholder_IsNotExpanded()
    {
        var entry = MakeEntry(ActivityLogType.Assignment, ActivityLogSubtype.Resource, ActivityLogTrigger.Created);
        entry.ToName = "{from}";

        var dto = DtoMapper.Convert(entry);

        dto.Text.Should().StartWith("{from} fikk enkelttjenesten");
    }

    [Fact]
    public void Convert_EventWithoutCatalogEntry_HasNullText()
    {
        var entry = MakeEntry(ActivityLogType.Assignment, null, ActivityLogTrigger.Updated);

        var dto = DtoMapper.Convert(entry);

        dto.ActivityTypeId.Should().BeNull();
        dto.Text.Should().BeNull();
    }

    // ── Catalog templates ──────────────────────────────────────────────────────
    public static TheoryData<Guid> CatalogEntries()
        => new(ActivityTypeConstants.AllEntities().Select(d => d.Id));

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void Catalog_EveryEntryHasTextInAllLanguages(Guid id)
    {
        ActivityTypeConstants.TryGetById(id, out var definition).Should().BeTrue();

        definition!.Entity.Text.Should().NotBeNullOrWhiteSpace();
        definition.NN.Translations.Should().ContainKey("Text").WhoseValue.Should().NotBeNullOrWhiteSpace();
        definition.EN.Translations.Should().ContainKey("Text").WhoseValue.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void Catalog_TextUsesTheSameKnownPlaceholdersInAllLanguages(Guid id)
    {
        ActivityTypeConstants.TryGetById(id, out var definition).Should().BeTrue();

        var bokmal = Placeholders(definition!.Entity.Text);
        bokmal.Should().BeSubsetOf(KnownPlaceholders);
        Placeholders(definition.NN.Translations["Text"]).Should().BeEquivalentTo(bokmal);
        Placeholders(definition.EN.Translations["Text"]).Should().BeEquivalentTo(bokmal);
    }

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void Catalog_TextOnlyNamesWhatTheEventCarries(Guid id)
    {
        ActivityTypeConstants.TryGetById(id, out var definition).Should().BeTrue();
        var entity = definition!.Entity;
        var placeholders = Placeholders(entity.Text);

        if (placeholders.Contains("via"))
        {
            entity.Type.Should().Be(ActivityLogType.Delegation);
        }

        if (placeholders.Contains("package"))
        {
            entity.Subtype.Should().Be(ActivityLogSubtype.Package);
        }

        if (placeholders.Contains("resource"))
        {
            entity.Subtype.Should().BeOneOf(ActivityLogSubtype.Resource, ActivityLogSubtype.Instance);
        }
    }

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void Catalog_RenderedTextLeavesNoPlaceholders(Guid id)
    {
        ActivityTypeConstants.TryGetById(id, out var definition).Should().BeTrue();
        var entity = definition!.Entity;
        var entry = MakeEntry(entity.Type, entity.Subtype, entity.Trigger, entity.Status);

        foreach (var language in Languages)
        {
            var dto = DtoMapper.Convert(entry, language);

            dto.ActivityTypeId.Should().Be(id);
            dto.Text.Should().NotContainAny("{", "}");
        }
    }

    private static List<string> Placeholders(string template)
        => Placeholder().Matches(template).Select(m => m.Groups[1].Value).ToList();

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex Placeholder();
}
