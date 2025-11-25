using System.Diagnostics.CodeAnalysis;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// Defines constant <see cref="Package"/> instances used across the system.
/// Each package represents a specific access package in the access management domain,
/// with a fixed unique identifier (GUID), localized names, descriptions, area references, and provider information.
/// </summary>
public static class PackageConstants
{
    /// <summary>
    /// Try to get <see cref="Package"/> by name.
    /// </summary>
    public static bool TryGetByName(string name, [NotNullWhen(true)] out ConstantDefinition<Package>? result)
        => ConstantLookup.TryGetByName(typeof(PackageConstants), name, out result);

    /// <summary>
    /// Try to get <see cref="Package"/> using Guid.
    /// </summary>
    public static bool TryGetById(Guid id, [NotNullWhen(true)] out ConstantDefinition<Package>? result)
        => ConstantLookup.TryGetById(typeof(PackageConstants), id, out result);

    /// <summary>
    /// Try to get <see cref="Role"/> by Urn.
    /// </summary>
    public static bool TryGetByUrn(string urn, [NotNullWhen(true)] out ConstantDefinition<Package>? result)
    {
        if (string.IsNullOrEmpty(urn))
        {
            result = null;
            return false;
        }

        urn = urn.ToLowerInvariant();

        // Case 1: already a full URN
        if (ConstantLookup.TryGetByUrn(typeof(PackageConstants), urn, out result))
        {
            return true;
        }

        // Case 2: Suffix only with ':'
        if (urn.StartsWith(':') && urn.Split(':').Length == 1)
        {
            if (ConstantLookup.TryGetByUrn(typeof(PackageConstants), $"urn:altinn:accesspackage{urn}", out result))
            {
                return true;
            }
        }

        // Case 3: Suffix only without ':'
        if (ConstantLookup.TryGetByUrn(typeof(PackageConstants), $"urn:altinn:accesspackage:{urn}", out result))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get all constants as a read-only collection.
    /// </summary>
    public static IReadOnlyCollection<ConstantDefinition<Package>> AllEntities()
        => ConstantLookup.AllEntities<Package>(typeof(PackageConstants));

    /// <summary>
    /// Get all translations as read-only collection.
    /// </summary>
    public static IReadOnlyCollection<TranslationEntry> AllTranslations()
        => ConstantLookup.AllTranslations<Package>(typeof(PackageConstants));

    #region Jordbruk, skogbruk, jakt, fiske og akvakultur

    /// <summary>
    /// Represents the 'Jordbruk' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> c5bbbc3f-605a-4dcb-a587-32124d7bb76d
    /// - <c>URN:</c> urn:altinn:accesspackage:jordbruk
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir tilgang til tjenester knyttet til jordbruk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Agriculture { get; } = new ConstantDefinition<Package>("c5bbbc3f-605a-4dcb-a587-32124d7bb76d")
    {
        Entity = new()
        {
            Name = "Jordbruk",
            Description = "Denne tilgangspakken gir tilgang til tjenester knyttet til jordbruk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:jordbruk",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AgricultureForestryHuntingFishingAndAquaculture,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Agriculture"),
            KeyValuePair.Create("Description", "This access package provides access to services related to agriculture. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Jordbruk"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir tilgang til tenester knytt til jordbruk. Ved regelverksendringer eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Dyrehold' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7b8a3aaa-c8ed-4ac4-923a-335f4f9eb45a
    /// - <c>URN:</c> urn:altinn:accesspackage:dyrehold
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir tilgang til tjenester knyttet til dyrehold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> AnimalHusbandry { get; } = new ConstantDefinition<Package>("7b8a3aaa-c8ed-4ac4-923a-335f4f9eb45a")
    {
        Entity = new()
        {
            Name = "Dyrehold",
            Description = "Denne tilgangspakken gir tilgang til tjenester knyttet til dyrehold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:dyrehold",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AgricultureForestryHuntingFishingAndAquaculture,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Animal husbandry"),
            KeyValuePair.Create("Description", "This access package provides access to services related to animal husbandry. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Dyrehald"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir tilgang til tenester knytt til dyrehald. Ved regelverksendringer eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Akvakultur' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 78c21107-7d2d-4e85-af82-47ea0e47ceca
    /// - <c>URN:</c> urn:altinn:accesspackage:akvakultur
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til akvakultur og fiskeoppdrett. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Aquaculture { get; } = new ConstantDefinition<Package>("78c21107-7d2d-4e85-af82-47ea0e47ceca")
    {
        Entity = new()
        {
            Name = "Akvakultur",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til akvakultur og fiskeoppdrett. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:akvakultur",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AgricultureForestryHuntingFishingAndAquaculture,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Aquaculture"),
            KeyValuePair.Create("Description", "This access package provides provides access to services related to aquaculture and fish farming. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that this access package provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Akvakultur"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til akvakultur og fiskeoppdrett. Ved regelverksendringer eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Fiske' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 9d2ec6e9-5148-4f47-9ae4-4536f6c9c1cb
    /// - <c>URN:</c> urn:altinn:accesspackage:fiske
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til fiske. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Fishing { get; } = new ConstantDefinition<Package>("9d2ec6e9-5148-4f47-9ae4-4536f6c9c1cb")
    {
        Entity = new()
        {
            Name = "Fiske",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til fiske. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:fiske",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AgricultureForestryHuntingFishingAndAquaculture,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Fishing"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to fishing. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Fiske"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til fiske. Ved regelverksendringer eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Skogbruk' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> f7e02568-90b6-477d-8abb-44984ddeb1f9
    /// - <c>URN:</c> urn:altinn:accesspackage:skogbruk
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til skogbruk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Forestry { get; } = new ConstantDefinition<Package>("f7e02568-90b6-477d-8abb-44984ddeb1f9")
    {
        Entity = new()
        {
            Name = "Skogbruk",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til skogbruk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:skogbruk",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AgricultureForestryHuntingFishingAndAquaculture,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Forestry"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to forestry. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Skogbruk"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til skogbruk. Ved regelverksendringer eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Jakt og viltstell' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 906aec0d-ad1f-496b-a0bb-40f81b3303cb
    /// - <c>URN:</c> urn:altinn:accesspackage:jakt-og-viltstell
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til jakt og viltstell. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> HuntingAndWildlifeManagement { get; } = new ConstantDefinition<Package>("906aec0d-ad1f-496b-a0bb-40f81b3303cb")
    {
        Entity = new()
        {
            Name = "Jakt og viltstell",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til jakt og viltstell. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:jakt-og-viltstell",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AgricultureForestryHuntingFishingAndAquaculture,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Hunting and wildlife management"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to hunting and wildlife management. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Jakt og viltstell"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til jakt og viltstell. Ved regelverksendringer eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Reindrift' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0bb5963e-df17-4f35-b913-3ce10a34b866
    /// - <c>URN:</c> urn:altinn:accesspackage:reindrift
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir tilgang til tjenester knyttet til reindrift. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> ReindeerHerding { get; } = new ConstantDefinition<Package>("0bb5963e-df17-4f35-b913-3ce10a34b866")
    {
        Entity = new()
        {
            Name = "Reindrift",
            Description = "Denne tilgangspakken gir tilgang til tjenester knyttet til reindrift. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:reindrift",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AgricultureForestryHuntingFishingAndAquaculture,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Reindeer herding"),
            KeyValuePair.Create("Description", "This access package provides access to services related to reindeer herding. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Reindrift"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir tilgang til tenester knytt til reindrift. Ved regelverksendringer eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Fullmakter for regnskapsfører

    /// <summary>
    /// Represents the 'Regnskapsfører med signeringsrettighet' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 955d5779-3e2b-4098-b11d-0431dc41ddbe
    /// - <c>URN:</c> urn:altinn:accesspackage:regnskapsforer-med-signeringsrettighet
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til regnskapfører å kunne signere på vegne av kunden for alle tjenester som krever signeringsrett. Dette er tjenester som man har vurdert det som naturlig at en regnskapsfører utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte regnskapsførere. Fullmakt hos regnskapfører oppstår når kunden registrerer regnskapsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> AuditorEmployee { get; } = new ConstantDefinition<Package>("955d5779-3e2b-4098-b11d-0431dc41ddbe")
    {
        Entity = new()
        {
            Name = "Regnskapsfører med signeringsrettighet",
            Description = "Denne fullmakten gir tilgang til regnskapfører å kunne signere på vegne av kunden for alle tjenester som krever signeringsrett. Dette er tjenester som man har vurdert det som naturlig at en regnskapsfører utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte regnskapsførere. Fullmakt hos regnskapfører oppstår når kunden registrerer regnskapsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:regnskapsforer-med-signeringsrettighet",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = false,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AuthorizationsForAccountants,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Accountant with signing rights"),
            KeyValuePair.Create("Description", "This authorization allows an accountant to sign on behalf of the client for all services that require signing rights. These are services that have been deemed natural for an accountant to perform on behalf of their client. The authorization is only granted to authorized accountants. Authorization to the accountant occurs when the client registers the accountant in the Central Coordinating Register for Legal Entities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Rekneskapsførar med signeringsrett"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til rekneskapsførar å kunne signere på vegne av kunden for alle tenester som krev signeringsrett. Dette er tenester som ein har vurdert det som naturleg at ein rekneskapsførar utfører på vegne av sin kunde. Fullmakta blir gitt berre til autoriserte rekneskapsførarar. Fullmakt hos rekneskapsførar oppstår når kunden registrerer rekneskapsførar i Einingsregisteret. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Regnskapsfører lønn' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 43becc6a-8c6c-4e9e-bb2f-08fe588ada21
    /// - <c>URN:</c> urn:altinn:accesspackage:regnskapsforer-lonn
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til regnskapsfører å rapportere lønn for sin kunde. Dette er tjenester som man har vurdert det som naturlig at en regnskapsfører utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte regnskapsførere. Fullmakt hos regnskapfører oppstår når kunden registrerer regnskapsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> CentralCoordinationRegister { get; } = new ConstantDefinition<Package>("43becc6a-8c6c-4e9e-bb2f-08fe588ada21")
    {
        Entity = new()
        {
            Name = "Regnskapsfører lønn",
            Description = "Denne fullmakten gir tilgang til regnskapsfører å rapportere lønn for sin kunde. Dette er tjenester som man har vurdert det som naturlig at en regnskapsfører utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte regnskapsførere. Fullmakt hos regnskapfører oppstår når kunden registrerer regnskapsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:regnskapsforer-lonn",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = false,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AuthorizationsForAccountants,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Accountant salary"),
            KeyValuePair.Create("Description", "This authorization allows an accountant to report salaries for their client. These are services that have been deemed natural for an accountant to perform on behalf of their client. The authorization is only granted to authorized accountants. Authorization to the accountant occurs when the client registers the accountant in the Central Coordinating Register for Legal Entities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Rekneskapsførar løn"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til rekneskapsførar å rapportere løn for sin kunde. Dette er tenester som ein har vurdert det som naturleg at ein rekneskapsførar utfører på vegne av sin kunde. Fullmakta blir gitt berre til autoriserte rekneskapsførarar. Fullmakt hos rekneskapsførar oppstår når kunden registrerer rekneskapsførar i Einingsregisteret. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Regnskapsfører uten signeringsrettighet' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> a5f7f72a-9b89-445d-85bb-06f678a3d4d1
    /// - <c>URN:</c> urn:altinn:accesspackage:regnskapsforer-uten-signeringsrettighet
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til å kunne utføre alle tjenester som ikke krever signeringsrett. Dette er tjenester som man har vurdert det som naturlig at en regnskapsfører utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte regnskapsførere. Fullmakt hos regnskapfører oppstår når kunden registrerer regnskapsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> PopulationRegistry { get; } = new ConstantDefinition<Package>("a5f7f72a-9b89-445d-85bb-06f678a3d4d1")
    {
        Entity = new()
        {
            Name = "Regnskapsfører uten signeringsrettighet",
            Description = "Denne fullmakten gir tilgang til å kunne utføre alle tjenester som ikke krever signeringsrett. Dette er tjenester som man har vurdert det som naturlig at en regnskapsfører utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte regnskapsførere. Fullmakt hos regnskapfører oppstår når kunden registrerer regnskapsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:regnskapsforer-uten-signeringsrettighet",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = false,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AuthorizationsForAccountants,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Accountant without signing rights"),
            KeyValuePair.Create("Description", "This authorization allows to perform all services that do not require signing rights. These are services that have been deemed natural for an accountant to perform on behalf of their client. The authorization is only granted to authorized accountants. Authorization to the accountant occurs when the client registers the accountant in the Central Coordinating Register for Legal Entities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Rekneskapsførar utan signeringsrett"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til å kunne utføre alle tenester som ikkje krev signeringsrett. Dette er tenester som ein har vurdert det som naturleg at ein rekneskapsførar utfører på vegne av sin kunde. Fullmakta blir gitt berre til autoriserte rekneskapsførarar. Fullmakt hos rekneskapsførar oppstår når kunden registrerer rekneskapsførar i Einingsregisteret. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Fullmakter for revisor

    /// <summary>
    /// Represents the 'Revisormedarbeider' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 96120c32-389d-46eb-8212-0a6540540c25
    /// - <c>URN:</c> urn:altinn:accesspackage:revisormedarbeider
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir revisor tilgang til å opptre som revisormedarbeider for en kunde og utføre alle tjenester som krever denne fullmakten. Dette er tjenester som tjenestetilbyder har vurdert det som naturlig at en revisor utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte revisorer. Fullmakt hos revisor oppstår når kunden registrerer revisor i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> EnforcementOfficer { get; } = new ConstantDefinition<Package>("96120c32-389d-46eb-8212-0a6540540c25")
    {
        Entity = new()
        {
            Urn = "urn:altinn:accesspackage:revisormedarbeider",
            Name = "Revisormedarbeider",
            Description = "Denne fullmakten gir revisor tilgang til å opptre som revisormedarbeider for en kunde og utføre alle tjenester som krever denne fullmakten. Dette er tjenester som tjenestetilbyder har vurdert det som naturlig at en revisor utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte revisorer. Fullmakt hos revisor oppstår når kunden registrerer revisor i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = false,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AuthorizationsForAuditors
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Assistant auditor"),
            KeyValuePair.Create("Description", "This authorization allows an auditor to act as an assistant auditor for a client and perform all services that require this authorization. These are services that the service provider has deemed natural for an auditor to perform on behalf of their client. The authorization is only granted to authorized auditors. Authorization to the auditor occurs when the client registers the auditor in the Central Coordinating Register for Legal Entities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Revisormedarbeidar"),
            KeyValuePair.Create("Description", "Denne fullmakta gir revisor tilgang til å opptre som revisormedarbeidar for ein kunde og utføre alle tenester som krev denne fullmakta. Dette er tenester som tenestytilbydar har vurdert det som naturleg at ein revisor utfører på vegne av sin kunde. Fullmakta blir gitt berre til autoriserte revisorar. Fullmakt hos revisor oppstår når kunden registrerer revisor i Einingsregisteret. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Ansvarlig revisor' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 2f176732-b1e9-449b-9918-090d1fa986f6
    /// - <c>URN:</c> urn:altinn:accesspackage:ansvarlig-revisor
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir revisor tilgang til å opptre som ansvarlig revisor for en kunde og utføre alle tjenester som krever denne fullmakten. Dette er tjenester som tjenestetilbyder har vurdert det som naturlig at en revisor utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte revisorer. Fullmakt hos revisor oppstår når kunden registrerer revisor i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> PersonalIdentityRegistry { get; } = new ConstantDefinition<Package>("2f176732-b1e9-449b-9918-090d1fa986f6")
    {
        Entity = new()
        {
            Name = "Ansvarlig revisor",
            Description = "Denne fullmakten gir revisor tilgang til å opptre som ansvarlig revisor for en kunde og utføre alle tjenester som krever denne fullmakten. Dette er tjenester som tjenestetilbyder har vurdert det som naturlig at en revisor utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte revisorer. Fullmakt hos revisor oppstår når kunden registrerer revisor i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:ansvarlig-revisor",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = false,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AuthorizationsForAuditors,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Auditor in charge"),
            KeyValuePair.Create("Description", "This authorization allows an auditor to act as the auditor in charge for a client and perform all services that require this authorization. These are services that the service provider has deemed natural for an auditor to perform on behalf of their client. The authorization is only granted to authorized auditors. Authorization to the auditor occurs when the client registers the auditor in the Central Coordinating Register for Legal Entities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Ansvarleg revisor"),
            KeyValuePair.Create("Description", "Denne fullmakta gir revisor tilgang til å opptre som ansvarleg revisor for ein kunde og utføre alle tenester som krev denne fullmakta. Dette er tenester som tenestytilbydar har vurdert det som naturleg at ein revisor utfører på vegne av sin kunde. Fullmakta blir gitt berre til autoriserte revisorar. Fullmakt hos revisor oppstår når kunden registrerer revisor i Einingsregisteret. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Fullmakter for konkursbo

    /// <summary>
    /// Represents the 'Konkursbo skrivetilgang' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0e219609-02c6-44e6-9c80-fe2c1997940e
    /// - <c>URN:</c> urn:altinn:accesspackage:konkursbo-skrivetilgang
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir bostyrers medhjelper tilgang til å jobbe på vegne av bostyrer. Bostyrer delegerer denne fullmakten sammen med Konkursbo lesetilgang til medhjelper for hvert konkursbo.
    /// </remarks>
    public static ConstantDefinition<Package> BankruptcyEstateWriteAccess { get; } = new ConstantDefinition<Package>("0e219609-02c6-44e6-9c80-fe2c1997940e")
    {
        Entity = new()
        {
            Name = "Konkursbo skrivetilgang",
            Description = "Denne fullmakten gir bostyrers medhjelper tilgang til å jobbe på vegne av bostyrer. Bostyrer delegerer denne fullmakten sammen med Konkursbo lesetilgang til medhjelper for hvert konkursbo.",
            Urn = "urn:altinn:accesspackage:konkursbo-skrivetilgang",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AuthorizationsForBankruptcyEstates,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Bankruptcy estate write access"),
            KeyValuePair.Create("Description", "This authorization gives the estate administrator's assistant access to work on behalf of the estate administrator. The estate administrator delegates this authorization together with Bankruptcy Estate Read Access to the assistant for each bankruptcy estate.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Konkursbu skrivetilgang"),
            KeyValuePair.Create("Description", "Denne fullmakta gir bostyrar sin medhjelpар tilgang til å jobbe på vegne av bostyrar. Bostyrar delegerer denne fullmakta saman med Konkursbu lesetilgang til medhjelparen for kvart konkursbu.")
        ),
    };

    /// <summary>
    /// Represents the 'Konkursbo lesetilgang' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 5ef836c7-69cc-4ea8-84d6-fb933cc4fc5c
    /// - <c>URN:</c> urn:altinn:accesspackage:konkursbo-lesetilgang
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten delegeres til kreditorer og andre som skal ha lesetilgang til det enkelte konkursbo.
    /// </remarks>
    public static ConstantDefinition<Package> PrivateEnforcementOfficer { get; } = new ConstantDefinition<Package>("5ef836c7-69cc-4ea8-84d6-fb933cc4fc5c")
    {
        Entity = new()
        {
            Urn = "urn:altinn:accesspackage:konkursbo-lesetilgang",
            Name = "Konkursbo lesetilgang",
            Description = "Denne fullmakten delegeres til kreditorer og andre som skal ha lesetilgang til det enkelte konkursbo.",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AuthorizationsForBankruptcyEstates,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Bankruptcy estate read access"),
            KeyValuePair.Create("Description", "This authorization is delegated to creditors and others who should have read access to the individual bankruptcy estate.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Konkursbu lesetilgang"),
            KeyValuePair.Create("Description", "Denne fullmakta blir delegert til kreditorar og andre som skal ha lesetilgang til det enkelte konkursbuet.")
        ),
    };

    #endregion

    #region Fullmakter for forretningsfører

    /// <summary>
    /// Represents the 'Forretningsforer eiendom' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0195efb8-7c80-7cf2-bcc8-720a3fb39d44
    /// - <c>URN:</c> urn:altinn:accesspackage:forretningsforer-eiendom
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir forretningsfører for Borettslag og Eierseksjonssameie tilgang til å opptre på vegne av kunde, og utføre alle tjenester som krever denne fullmakten. Dette er tjenester som tjenestetilbyder har vurdert det som naturlig at en forretningsfører utfører på vegne av sin kunde. Fullmakt hos forretningsfører oppstår når Borettslaget eller Eierseksjonssameiet registrerer forretningsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> BusinessManagerRealEstate { get; } = new ConstantDefinition<Package>("0195efb8-7c80-7cf2-bcc8-720a3fb39d44")
    {
        Entity = new()
        {
            Urn = "urn:altinn:accesspackage:forretningsforer-eiendom",
            Name = "Forretningsforer eiendom",
            Description = "Denne fullmakten gir forretningsfører for Borettslag og Eierseksjonssameie tilgang til å opptre på vegne av kunde, og utføre alle tjenester som krever denne fullmakten. Dette er tjenester som tjenestetilbyder har vurdert det som naturlig at en forretningsfører utfører på vegne av sin kunde. Fullmakt hos forretningsfører oppstår når Borettslaget eller Eierseksjonssameiet registrerer forretningsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = false,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.AuthorizationsForBusinesses,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Business manager real estate"),
            KeyValuePair.Create("Description", "This authorization gives the business manager for housing cooperatives and condominium associations access to act on behalf of the client and perform all services that require this authorization. These are services that the service provider has deemed natural for a business manager to perform on behalf of their client. Authorization to the business manager occurs when the housing cooperative or condominium association registers the business manager in the Central Coordinating Register for Legal Entities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Forretningsførar eigedom"),
            KeyValuePair.Create("Description", "Denne fullmakta gir forretningsførar for borettslag og eiarseksjonssameige tilgang til å opptre på vegne av kunde, og utføre alle tenester som krev denne fullmakta. Dette er tenester som tenestytilbydar har vurdert det som naturleg at ein forretningsførar utfører på vegne av sin kunde. Fullmakt hos forretningsførar oppstår når borettslaget eller eiarseksjonssameiet registrerer forretningsførar i Einingsregisteret. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Forhold ved virksomheten

    /// <summary>
    /// Represents the 'Attester' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4717e6e0-1ec2-4354-b825-d9a9e2588fb1
    /// - <c>URN:</c> urn:altinn:accesspackage:attester
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til attestering av virksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Certificates { get; } = new ConstantDefinition<Package>("4717e6e0-1ec2-4354-b825-d9a9e2588fb1")
    {
        Entity = new()
        {
            Name = "Attester",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til attestering av virksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:attester",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Certificates"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to certification of businesses. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Attestar"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til attestering av verksemd. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Dokumentbasert tilsyn' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> df13707e-5252-496a-bc00-daffeed1e4b2
    /// - <c>URN:</c> urn:altinn:accesspackage:dokumentbasert-tilsyn
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til dokumentbaserte tilsyn. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> DocumentBasedSupervision { get; } = new ConstantDefinition<Package>("df13707e-5252-496a-bc00-daffeed1e4b2")
    {
        Entity = new()
        {
            Name = "Dokumentbasert tilsyn",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til dokumentbaserte tilsyn. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:dokumentbasert-tilsyn",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Document-based supervision"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to document-based supervision. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Dokumentbasert tilsyn"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til dokumentbaserte tilsyn. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Eksplisitt tjenestedelegering' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> c0eb20c1-2268-48f5-88c5-f26cb47a6b1f
    /// - <c>URN:</c> urn:altinn:accesspackage:eksplisitt
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten er ikke delegerbar, og er ikke knyttet til noen roller i ENhetsregisteret. Tilgang til tjenester knyttet til denne pakken kan gis av Hovedadministrator gjennom enkeltrettighetsdelegering.
    /// </remarks>
    public static ConstantDefinition<Package> ExplicitServiceDelegation { get; } = new ConstantDefinition<Package>("c0eb20c1-2268-48f5-88c5-f26cb47a6b1f")
    {
        Entity = new()
        {
            Name = "Eksplisitt tjenestedelegering",
            Description = "Denne fullmakten er ikke delegerbar, og er ikke knyttet til noen roller i ENhetsregisteret. Tilgang til tjenester knyttet til denne pakken kan gis av Hovedadministrator gjennom enkeltrettighetsdelegering.",
            Urn = "urn:altinn:accesspackage:eksplisitt",
            IsDelegable = false,
            HasResources = true,
            IsAssignable = false,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Explicit service delegation"),
            KeyValuePair.Create("Description", "This authorization is not delegable and is not linked to any roles in the Central Coordinating Register for Legal Entities. Access to services associated with this package can be granted by the Main Administrator through individual rights delegation.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Eksplisitt tenestedelegering"),
            KeyValuePair.Create("Description", "Denne fullmakta er ikkje delegerbar, og er ikkje knytt til nokon roller i Einingsregisteret. Tilgang til tenester knytt til denne pakken kan bli gitt av hovudadministrator gjennom enkeltrettigheitsdelegering.")
        ),
    };

    /// <summary>
    /// Represents the 'Generelle helfotjenester' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 06c13400-8683-4985-9802-cef13e247f24
    /// - <c>URN:</c> urn:altinn:accesspackage:generelle-helfotjenester
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til ordinære tjenester knyttet til virksomhetens dialog med Helfo der bruker kan få tilgang til personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> GeneralHelfoServices { get; } = new ConstantDefinition<Package>("06c13400-8683-4985-9802-cef13e247f24")
    {
        Entity = new()
        {
            Name = "Generelle helfotjenester",
            Description = "Denne fullmakten gir tilgang til ordinære tjenester knyttet til virksomhetens dialog med Helfo der bruker kan få tilgang til personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:generelle-helfotjenester",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "General Helfo services"),
            KeyValuePair.Create("Description", "This authorization provides access to regular services related to the business's dialogue with Helfo where the user can gain access to personal information about persons being reported on. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Generelle helfotenester"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til ordinære tenester knytt til verksemda sin dialog med Helfo der brukar kan få tilgang til personopplysningar om personar det blir rapportert om. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Tilskudd, støtte og erstatning' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> d3ba7bba-195b-4b69-ae68-df20ecd57097
    /// - <c>URN:</c> urn:altinn:accesspackage:tilskudd-stotte-erstatning
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til å søke tilskudd, støtte og erstatning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> GrantSupportCompensation { get; } = new ConstantDefinition<Package>("d3ba7bba-195b-4b69-ae68-df20ecd57097")
    {
        Entity = new()
        {
            Name = "Tilskudd, støtte og erstatning",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til å søke tilskudd, støtte og erstatning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:tilskudd-stotte-erstatning",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Grant, support and compensation"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to applying for grants, support and compensation. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tilskot, støtte og erstatning"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til å søkje tilskot, støtte og erstatning. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Helfotjenester med personopplysninger av særlig kategori' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 2ee6e972-2423-4f3a-bd3c-d4871fcc9876
    /// - <c>URN:</c> urn:altinn:accesspackage:helfo-saerlig-kategori
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til virksomhetens dialog med Helfo der bruker kan få tilgang til personopplysninger av særlig kategori. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir
    /// </remarks>
    public static ConstantDefinition<Package> HelfoServicesSpecialCategory { get; } = new ConstantDefinition<Package>("2ee6e972-2423-4f3a-bd3c-d4871fcc9876")
    {
        Entity = new()
        {
            Name = "Helfotjenester med personopplysninger av særlig kategori",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til virksomhetens dialog med Helfo der bruker kan få tilgang til personopplysninger av særlig kategori. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir",
            Urn = "urn:altinn:accesspackage:helfo-saerlig-kategori",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Helfo services with personal data of special category"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to the business's dialogue with Helfo where the user can gain access to personal data of special categories. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Helfotenester med personopplysningar av særleg kategori"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til verksemda sin dialog med Helfo der brukar kan få tilgang til personopplysningar av særleg kategori. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Infrastruktur' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 75978efe-2437-421e-8c77-dd61925c7ba4
    /// - <c>URN:</c> urn:altinn:accesspackage:infrastruktur
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til virksomhetens infrastruktur. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Infrastructure { get; } = new ConstantDefinition<Package>("75978efe-2437-421e-8c77-dd61925c7ba4")
    {
        Entity = new()
        {
            Name = "Infrastruktur",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til virksomhetens infrastruktur. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:infrastruktur",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Infrastructure"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to the business's infrastructure. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Infrastruktur"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til verksemda sin infrastruktur. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Mine sider hos kommunen' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 600bf1be-61f6-423c-9a13-df93ee3214a5
    /// - <c>URN:</c> urn:altinn:accesspackage:mine-sider-kommune
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir generell tilgang til tjenester av typen “mine sider” tjenester hos kommuner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> MunicipalServices { get; } = new ConstantDefinition<Package>("600bf1be-61f6-423c-9a13-df93ee3214a5")
    {
        Entity = new()
        {
            Name = "Mine sider hos kommunen",
            Description = "Denne fullmakten gir generell tilgang til tjenester av typen “mine sider” tjenester hos kommuner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:mine-sider-kommune",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "My pages at the municipality"),
            KeyValuePair.Create("Description", "This authorization provides general access to \"my page\" type services at municipalities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Mine sider hos kommunen"),
            KeyValuePair.Create("Description", "Denne fullmakta gir generell tilgang til tenester av typen «mine side» tenester hos kommunar. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Patent, varemerke og design' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 250568e6-ed9f-4bdc-b9e3-df08be7181ba
    /// - <c>URN:</c> urn:altinn:accesspackage:patent-varemerke-design
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til patent, varemerke og design. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> PatentTrademarkDesign { get; } = new ConstantDefinition<Package>("250568e6-ed9f-4bdc-b9e3-df08be7181ba")
    {
        Entity = new()
        {
            Name = "Patent, varemerke og design",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til patent, varemerke og design. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:patent-varemerke-design",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Patents, trademarks and design"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to patents, trademarks and design. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Patent, varemerke og design"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til patent, varemerke og design. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Politi og domstol' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 55e73960-09e5-473e-aa40-deaf97cf9bf2
    /// - <c>URN:</c> urn:altinn:accesspackage:politi-og-domstol
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til virksomhetens dialog om juridiske forhold med politi og jusitsmyndigheter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> PoliceAndCourt { get; } = new ConstantDefinition<Package>("55e73960-09e5-473e-aa40-deaf97cf9bf2")
    {
        Entity = new()
        {
            Name = "Politi og domstol",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til virksomhetens dialog om juridiske forhold med politi og jusitsmyndigheter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:politi-og-domstol",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Police and courts"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to the business's dialogue on legal matters with police and judicial authorities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Politi og domstol"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til verksemda sin dialog om juridiske forhold med politi og justismyndigheiter. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Folkeregister' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 5b51658e-f716-401c-9fc2-fe70bbe70f48
    /// - <c>URN:</c> urn:altinn:accesspackage:folkeregister
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakt til tjenester som en virksomhet kan ha mot folkeregister. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> PopulationRegistryBusiness { get; } = new ConstantDefinition<Package>("5b51658e-f716-401c-9fc2-fe70bbe70f48")
    {
        Entity = new()
        {
            Name = "Folkeregister",
            Description = "Denne tilgangspakken gir fullmakt til tjenester som en virksomhet kan ha mot folkeregister. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:folkeregister",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Population register"),
            KeyValuePair.Create("Description", "This access package provides authorization for services that a business can have with the population register. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Folkeregister"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakt til tenester som ei verksemd kan ha mot folkeregisteret. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Forskning' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 594a2c4b-47a1-48c6-a01c-ed44ec4c05a4
    /// - <c>URN:</c> urn:altinn:accesspackage:forskning
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester knyttet til forskning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Research { get; } = new ConstantDefinition<Package>("594a2c4b-47a1-48c6-a01c-ed44ec4c05a4")
    {
        Entity = new()
        {
            Name = "Forskning",
            Description = "Denne fullmakten gir tilgang til tjenester knyttet til forskning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:forskning",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Research"),
            KeyValuePair.Create("Description", "This authorization provides access to services related to research. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Forsking"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester knytt til forsking. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Aksjer og eierforhold' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7dfc669b-70e4-4974-9e11-d6dca4803aaa
    /// - <c>URN:</c> urn:altinn:accesspackage:aksjer-og-eierforhold
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til aksjer og eierforhold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> SharesAndOwnership { get; } = new ConstantDefinition<Package>("7dfc669b-70e4-4974-9e11-d6dca4803aaa")
    {
        Entity = new()
        {
            Name = "Aksjer og eierforhold",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til aksjer og eierforhold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:aksjer-og-eierforhold",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Shares and ownership"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to shares and ownership. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Aksjar og eigarforhold"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til aksjar og eigarforhold. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Starte, endre og avvikle virksomhet' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 956cfcf0-ab4c-44d8-98a2-d68c4d59321b
    /// - <c>URN:</c> urn:altinn:accesspackage:starte-drive-endre-avikle-virksomhet
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til å starte, endre og avvikle en virksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> StartChangeDissolveEBusiness { get; } = new ConstantDefinition<Package>("956cfcf0-ab4c-44d8-98a2-d68c4d59321b")
    {
        Entity = new()
        {
            Name = "Starte, endre og avvikle virksomhet",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til å starte, endre og avvikle en virksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:starte-drive-endre-avikle-virksomhet",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Start, change and dissolve business"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to starting, changing and dissolving a business. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Starte, endre og avvikle verksemd"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til å starte, endre og avvikle ei verksemd. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Rapportering av statistikk' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ac54a5ca-16d2-4132-ae0d-e5aa8bd1ff6e
    /// - <c>URN:</c> urn:altinn:accesspackage:rapportering-statistikk
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til alle pålagte rapportering av statistikk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> StatisticsReporting { get; } = new ConstantDefinition<Package>("ac54a5ca-16d2-4132-ae0d-e5aa8bd1ff6e")
    {
        Entity = new()
        {
            Name = "Rapportering av statistikk",
            Description = "Denne fullmakten gir tilgang til alle pålagte rapportering av statistikk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:rapportering-statistikk",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.BusinessAffairs,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Statistics reporting"),
            KeyValuePair.Create("Description", "This authorization provides access to all mandatory statistics reporting. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Rapportering av statistikk"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til alle pålagde rapportering av statistikk. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Oppvekst og utdanning

    /// <summary>
    /// Represents the 'SFO-leder' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 219ea5f7-eb30-424f-a958-67d3c7e7a4c2
    /// - <c>URN:</c> urn:altinn:accesspackage:sfo-leder
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til førskole og fritidsordning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> AfterSchoolProgramLeader { get; } = new ConstantDefinition<Package>("219ea5f7-eb30-424f-a958-67d3c7e7a4c2")
    {
        Entity = new()
        {
            Name = "SFO-leder",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til førskole og fritidsordning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:sfo-leder",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "After-school program leader"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to preschool and after-school programs. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "SFO-leiar"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til førskule og fritidsordning. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Godkjenning av utdanningsvirksomhet' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> b56d2614-3f9d-4d93-8a8e-64d80b654ad7
    /// - <c>URN:</c> urn:altinn:accesspackage:godkjenning-av-utdanningsvirksomhet
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til å søke om godkjenning av utdanningsvirksomheter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> EducationalInstitutionApproval { get; } = new ConstantDefinition<Package>("b56d2614-3f9d-4d93-8a8e-64d80b654ad7")
    {
        Entity = new()
        {
            Name = "Godkjenning av utdanningsvirksomhet",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til å søke om godkjenning av utdanningsvirksomheter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:godkjenning-av-utdanningsvirksomhet",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Educational institution approval"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to applying for approval of educational institutions. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Godkjenning av utdanningsverksemd"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til å søkje om godkjenning av utdanningsverksemder. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Statsforvalter - barnehage' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7e6d8dd4-35a3-49d6-a2c5-73cd35018646
    /// - <c>URN:</c> urn:altinn:accesspackage:statsforvalter-barnehage
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester for statsforvalter knyttet til barnehagesektor.
    /// </remarks>
    public static ConstantDefinition<Package> GovernorKindergartenAuthority { get; } = new ConstantDefinition<Package>("7e6d8dd4-35a3-49d6-a2c5-73cd35018646")
    {
        Entity = new()
        {
            Name = "Statsforvalter - barnehage",
            Description = "Denne fullmakten gir tilgang til tjenester for statsforvalter knyttet til barnehagesektor.",
            Urn = "urn:altinn:accesspackage:statsforvalter-barnehage",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "County governor - kindergarten"),
            KeyValuePair.Create("Description", "This authorization provides access to services for the county governor related to the kindergarten sector.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Statsforvaltar - barnehage"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester for statsforvaltar knytt til barnehagsektoren.")
        ),
    };

    /// <summary>
    /// Represents the 'Statsforvalter - skole og opplæring' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> c1242df4-6af1-4e0b-b022-73c1099f5297
    /// - <c>URN:</c> urn:altinn:accesspackage:statsforvalter-skole-og-opplearing
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester for statsforvalter knyttet til skole- og opplæringssektor, herunder fagopplæring og voksenopplæring.
    /// </remarks>
    public static ConstantDefinition<Package> GovernorSchoolEducation { get; } = new ConstantDefinition<Package>("c1242df4-6af1-4e0b-b022-73c1099f5297")
    {
        Entity = new()
        {
            Name = "Statsforvalter - skole og opplæring",
            Description = "Denne fullmakten gir tilgang til tjenester for statsforvalter knyttet til skole- og opplæringssektor, herunder fagopplæring og voksenopplæring.",
            Urn = "urn:altinn:accesspackage:statsforvalter-skole-og-opplearing",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "County governor - school and education"),
            KeyValuePair.Create("Description", "This authorization provides access to services for the county governor related to the school and education sector, including vocational training and adult education.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Statsforvaltar - skule og opplæring"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester for statsforvaltar knytt til skule- og opplæringssektoren, medrekna fagopplæring og vaksenopplæring.")
        ),
    };

    /// <summary>
    /// Represents the 'Høyere utdanning og høyere yrkesfaglig utdanning' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 93f37a3d-f799-47b3-bc8e-675b5813abf4
    /// - <c>URN:</c> urn:altinn:accesspackage:hoyere-utdanning-og-hoyere-yrkesfaglig-utdanning
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til høyere utdanning og høyere yrkesfaglig utdanning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> HigherEducation { get; } = new ConstantDefinition<Package>("93f37a3d-f799-47b3-bc8e-675b5813abf4")
    {
        Entity = new()
        {
            Name = "Høyere utdanning og høyere yrkesfaglig utdanning",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til høyere utdanning og høyere yrkesfaglig utdanning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:hoyere-utdanning-og-hoyere-yrkesfaglig-utdanning",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Higher education and higher vocational education"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to higher education and higher vocational education. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Høgare utdanning og høgare yrkesfagleg utdanning"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til høgare utdanning og høgare yrkesfagleg utdanning. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Barnehagemyndighet' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0ecc481a-691c-496d-a3f2-748b8c450ed9
    /// - <c>URN:</c> urn:altinn:accesspackage:barnehagemyndighet
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester innen drift av barnehage som barnehagemyndighet er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> KindergartenAuthority { get; } = new ConstantDefinition<Package>("0ecc481a-691c-496d-a3f2-748b8c450ed9")
    {
        Entity = new()
        {
            Name = "Barnehagemyndighet",
            Description = "Denne fullmakten gir tilgang til tjenester innen drift av barnehage som barnehagemyndighet er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:barnehagemyndighet",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kindergarten authority"),
            KeyValuePair.Create("Description", "This authorization provides access to services for the operation of kindergartens that the kindergarten authority is responsible for. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Barnehagemyndigheit"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester innan drift av barnehage som barnehagemyndigheit er ansvarleg for. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Barnehageleder' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> dcb57f3e-0e5b-4ef7-a10c-74c53bc5a90d
    /// - <c>URN:</c> urn:altinn:accesspackage:barnehageleder
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester innen drift av barnehage som barnehageleder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> KindergartenLeader { get; } = new ConstantDefinition<Package>("dcb57f3e-0e5b-4ef7-a10c-74c53bc5a90d")
    {
        Entity = new()
        {
            Name = "Barnehageleder",
            Description = "Denne fullmakten gir tilgang til tjenester innen drift av barnehage som barnehageleder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:barnehageleder",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kindergarten leader"),
            KeyValuePair.Create("Description", "This authorization provides access to services for the operation of kindergartens that the kindergarten leader is responsible for. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Barnehageleder"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester innan drift av barnehage som barnehageleder er ansvarleg for. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Barnehageeier' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 9a14f2c1-f0bc-4965-97c0-76a4e191bbe1
    /// - <c>URN:</c> urn:altinn:accesspackage:barnehageeier
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester innen drift av barnehage som barnehageeier er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> KindergartenOwner { get; } = new ConstantDefinition<Package>("9a14f2c1-f0bc-4965-97c0-76a4e191bbe1")
    {
        Entity = new()
        {
            Name = "Barnehageeier",
            Description = "Denne fullmakten gir tilgang til tjenester innen drift av barnehage som barnehageeier er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:barnehageeier",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kindergarten owner"),
            KeyValuePair.Create("Description", "This authorization provides access to services for the operation of kindergartens that the kindergarten owner is responsible for. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Barnehageeigар"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester innan drift av barnehage som barnehageeigaren er ansvarleg for. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Opplæringskontorleder' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 40397a93-b047-4011-a6b8-6b8af16b6324
    /// - <c>URN:</c> urn:altinn:accesspackage:opplaeringskontorleder
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester innen drift av opplæringskontor som opplæringskontorleder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> OfficeLeader { get; } = new ConstantDefinition<Package>("40397a93-b047-4011-a6b8-6b8af16b6324")
    {
        Entity = new()
        {
            Urn = "urn:altinn:accesspackage:opplaeringskontorleder",
            Name = "Opplæringskontorleder",
            Description = "Denne fullmakten gir tilgang til tjenester innen drift av opplæringskontor som opplæringskontorleder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Training office leader"),
            KeyValuePair.Create("Description", "This authorization provides access to services for the operation of training offices that the training office leader is responsible for. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Opplæringskontorleder"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester innan drift av opplæringskontor som opplæringskontorleder er ansvarleg for. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'PPT-leder' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4cd69693-aff0-4e88-8b64-6b5620672468
    /// - <c>URN:</c> urn:altinn:accesspackage:ppt-leder
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester innen drift av Pedagogisk-psykologisk tjeneste (PPT) som PPT-leder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> PPTLeader { get; } = new ConstantDefinition<Package>("4cd69693-aff0-4e88-8b64-6b5620672468")
    {
        Entity = new()
        {
            Urn = "urn:altinn:accesspackage:ppt-leder",
            Name = "PPT-leder",
            Description = "Denne fullmakten gir tilgang til tjenester innen drift av Pedagogisk-psykologisk tjeneste (PPT) som PPT-leder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Educational-psychological service leader"),
            KeyValuePair.Create("Description", "This authorization provides access to services for the operation of the Educational-Psychological Service (PPT) that the PPT leader is responsible for. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "PPT-leiar"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester innan drift av Pedagogisk-psykologisk teneste (PPT) som PPT-leiar er ansvarleg for. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Godkjenning av personell' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 8cf4ec08-90dc-47d0-93f8-64a50c9b38b0
    /// - <c>URN:</c> urn:altinn:accesspackage:godkjenning-av-personell
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til å søke om godkjenning til enkeltpersoner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> PersonnelApproval { get; } = new ConstantDefinition<Package>("8cf4ec08-90dc-47d0-93f8-64a50c9b38b0")
    {
        Entity = new()
        {
            Name = "Godkjenning av personell",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til å søke om godkjenning til enkeltpersoner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:godkjenning-av-personell",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Personnel approval"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to applying for approval for individuals. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Godkjenning av personell"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til å søkje om godkjenning til enkeltpersonar. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Skoleleder' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 499d0e1c-18d7-4f21-b110-723e3d13003a
    /// - <c>URN:</c> urn:altinn:accesspackage:skoleleder
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester innen drift av skole som skoleleder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> SchoolLeader { get; } = new ConstantDefinition<Package>("499d0e1c-18d7-4f21-b110-723e3d13003a")
    {
        Entity = new()
        {
            Name = "Skoleleder",
            Description = "Denne fullmakten gir tilgang til tjenester innen drift av skole som skoleleder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:skoleleder",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "School leader"),
            KeyValuePair.Create("Description", "This authorization provides access to services for the operation of schools that the school leader is responsible for. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Skuleleder"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester innan drift av skule som skuleleder er ansvarleg for. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Skoleeier' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 32bfd131-1570-4b0c-888b-733cbb72d0cb
    /// - <c>URN:</c> urn:altinn:accesspackage:skoleeier
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester innen drift av skole som skoleeier er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> SchoolOwner { get; } = new ConstantDefinition<Package>("32bfd131-1570-4b0c-888b-733cbb72d0cb")
    {
        Entity = new()
        {
            Name = "Skoleeier",
            Description = "Denne fullmakten gir tilgang til tjenester innen drift av skole som skoleeier er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:skoleeier",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ChildhoodAndEducation,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "School owner"),
            KeyValuePair.Create("Description", "This authorization provides access to services for the operation of schools that the school owner is responsible for. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Skuleeigаr"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester innan drift av skule som skuleeigaren er ansvarleg for. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Handel, overnatting og servering

    /// <summary>
    /// Represents the 'Overnatting' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 77cdd23a-dddf-43e6-b5c2-0d8299b0888c
    /// - <c>URN:</c> urn:altinn:accesspackage:overnatting
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til overnattingsvirksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Accommodation { get; } = new ConstantDefinition<Package>("77cdd23a-dddf-43e6-b5c2-0d8299b0888c")
    {
        Entity = new()
        {
            Name = "Overnatting",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til overnattingsvirksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:overnatting",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.CommerceAccommodationAndCatering,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Accommodation"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to accommodation businesses. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Overnatting"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til overnattingsverksemd. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Servering' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 6710a7a0-78f9-47e3-bfa6-0d875869befd
    /// - <c>URN:</c> urn:altinn:accesspackage:servering
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til serveringsvirksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Catering { get; } = new ConstantDefinition<Package>("6710a7a0-78f9-47e3-bfa6-0d875869befd")
    {
        Entity = new()
        {
            Name = "Servering",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til serveringsvirksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:servering",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.CommerceAccommodationAndCatering,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Catering"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to catering businesses. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Servering"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til serveringsverksemd. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Varehandel' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> a6d704b7-d56b-4517-be79-0acd5b55b35e
    /// - <c>URN:</c> urn:altinn:accesspackage:varehandel
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til varehandel, inkludert engros- og detaljhandel, import og eksport, og salg og reparasjon av motorvogner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> RetailTrade { get; } = new ConstantDefinition<Package>("a6d704b7-d56b-4517-be79-0acd5b55b35e")
    {
        Entity = new()
        {
            Name = "Varehandel",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til varehandel, inkludert engros- og detaljhandel, import og eksport, og salg og reparasjon av motorvogner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:varehandel",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.CommerceAccommodationAndCatering,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Retail trade"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to retail trade, including wholesale and retail, import and export, and sale and repair of motor vehicles. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Varehandel"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til varehandel, inkludert eingongs- og detaljhandel, import og eksport, og sal og reparasjon av motorvogner. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Bygg, anlegg og eiendom

    /// <summary>
    /// Represents the 'Byggesøknad' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 1e40697b-e178-4920-8fbd-af5164b4d147
    /// - <c>URN:</c> urn:altinn:accesspackage:byggesoknad
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester som ansvarlig søker/tiltakshaver trenger, for eksempel byggesøknader, direkte signerte erklæringer, nabovarsel og eiendomssak. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> BuildingApplication { get; } = new ConstantDefinition<Package>("1e40697b-e178-4920-8fbd-af5164b4d147")
    {
        Entity = new()
        {
            Name = "Byggesøknad",
            Description = "Denne tilgangspakken gir fullmakter til tjenester som ansvarlig søker/tiltakshaver trenger, for eksempel byggesøknader, direkte signerte erklæringer, nabovarsel og eiendomssak. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:byggesoknad",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ConstructionInfrastructureAndRealEstate,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Building application"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services that the responsible applicant/project owner needs, such as building applications, directly signed declarations, neighbor notifications and property cases. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Byggesøknad"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester som ansvarleg søkjar/tiltakshavar treng, til dømes byggesøknader, direkte signerte erklæringar, nabovarsel og eigedomssak. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Kjøp og salg av eiendom' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> b98ec05d-1ac5-4ced-8250-b6d75b83502b
    /// - <c>URN:</c> urn:altinn:accesspackage:kjop-og-salg-eiendom
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til kjøp og salg av eiendom. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> BuyingSellungRealEstate { get; } = new ConstantDefinition<Package>("b98ec05d-1ac5-4ced-8250-b6d75b83502b")
    {
        Entity = new()
        {
            Name = "Kjøp og salg av eiendom",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til kjøp og salg av eiendom. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:kjop-og-salg-eiendom",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ConstructionInfrastructureAndRealEstate,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Buying and selling real estate"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to buying and selling real estate. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kjøp og sal av eigedom"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til kjøp og sal av eigedom. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Oppføring av bygg og anlegg' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ac655a2f-be29-4888-9a6c-b21e524fa90e
    /// - <c>URN:</c> urn:altinn:accesspackage:oppforing-bygg-anlegg
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester relatert til oppføring av bygninger og annlegg unntatt plan og byggesaksbehandling. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> ConstructionBuildingsFacilities { get; } = new ConstantDefinition<Package>("ac655a2f-be29-4888-9a6c-b21e524fa90e")
    {
        Entity = new()
        {
            Name = "Oppføring av bygg og anlegg",
            Description = "Denne tilgangspakken gir fullmakter til tjenester relatert til oppføring av bygninger og annlegg unntatt plan og byggesaksbehandling. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:oppforing-bygg-anlegg",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ConstructionInfrastructureAndRealEstate,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Construction of buildings and facilities"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to the construction of buildings and facilities, excluding planning and building case processing. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Oppføring av bygg og anlegg"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester relatert til oppføring av bygningar og anlegg unntatt plan og byggesaksbehandling. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Plansak' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 482eee1e-ec79-45bb-8bd0-af8459cbb9f0
    /// - <c>URN:</c> urn:altinn:accesspackage:plansak
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester som forslagsstiller/ plankonsulent trenger, for eksempel varsel om planopppstart og høring og offentlig ettersyn. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> PlanningCase { get; } = new ConstantDefinition<Package>("482eee1e-ec79-45bb-8bd0-af8459cbb9f0")
    {
        Entity = new()
        {
            Name = "Plansak",
            Description = "Denne tilgangspakken gir fullmakter til tjenester som forslagsstiller/ plankonsulent trenger, for eksempel varsel om planopppstart og høring og offentlig ettersyn. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:plansak",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ConstructionInfrastructureAndRealEstate,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Planning case"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services that the proposer/planning consultant needs, such as notification of planning commencement, public hearing and public review. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Plansak"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester som forslagsstillar/plankonsulent treng, til dømes varsel om planoppstart og høyring og offentleg ettersyn. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Tinglysing eiendom' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0195efb8-7c80-7642-b9b8-c748ee4fecd4
    /// - <c>URN:</c> urn:altinn:accesspackage:tinglysing-eiendom
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til elektronisk tinglysing av rettigheter i eiendom.
    /// </remarks>
    public static ConstantDefinition<Package> PropertyRegistration { get; } = new ConstantDefinition<Package>("0195efb8-7c80-7642-b9b8-c748ee4fecd4")
    {
        Entity = new()
        {
            Name = "Tinglysing eiendom",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til elektronisk tinglysing av rettigheter i eiendom.",
            Urn = "urn:altinn:accesspackage:tinglysing-eiendom",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ConstructionInfrastructureAndRealEstate,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Property registration"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to electronic registration of rights in real estate.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tinglysing eigedom"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til elektronisk tinglysing av rettar i eigedom.")
        ),
    };

    /// <summary>
    /// Represents the 'Eiendomsmegler' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 79329c4a-d856-4491-965e-bcc4ed5a7453
    /// - <c>URN:</c> urn:altinn:accesspackage:eiendomsmegler
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til omsetning og drift av fast eiendom på oppdrag, som eiendomsmegling og eiendomsforvaltning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> RealEstateAgent { get; } = new ConstantDefinition<Package>("79329c4a-d856-4491-965e-bcc4ed5a7453")
    {
        Entity = new()
        {
            Name = "Eiendomsmegler",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til omsetning og drift av fast eiendom på oppdrag, som eiendomsmegling og eiendomsforvaltning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:eiendomsmegler",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ConstructionInfrastructureAndRealEstate,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Real estate agent"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to the sale and operation of real estate on assignment, such as real estate brokerage and property management. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Eigedomsmeklar"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til omsetning og drift av fast eigedom på oppdrag, som eigedomsmekling og eigedomsforvaltning. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Utleie av eiendom' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 565a3110-3d51-4e72-ae4c-b89308e5c96e
    /// - <c>URN:</c> urn:altinn:accesspackage:utleie-eiendom
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til utleie av eiendom. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> RealEstateRental { get; } = new ConstantDefinition<Package>("565a3110-3d51-4e72-ae4c-b89308e5c96e")
    {
        Entity = new()
        {
            Name = "Utleie av eiendom",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til utleie av eiendom. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:utleie-eiendom",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ConstructionInfrastructureAndRealEstate,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Real estate rental"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to real estate rental. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Utleige av eigedom"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til utleige av eigedom. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Motta nabo- og planvarsel' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 1e1e8bf9-096f-4de2-9db7-b176db55db09
    /// - <c>URN:</c> urn:altinn:accesspackage:motta-nabo-og-planvarsel
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester til å lese og svare på varsel om plan-/byggesak. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> ReceiveNeighborPlanNotifications { get; } = new ConstantDefinition<Package>("1e1e8bf9-096f-4de2-9db7-b176db55db09")
    {
        Entity = new()
        {
            Name = "Motta nabo- og planvarsel",
            Description = "Denne tilgangspakken gir fullmakter til tjenester til å lese og svare på varsel om plan-/byggesak. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:motta-nabo-og-planvarsel",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ConstructionInfrastructureAndRealEstate,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Receive neighbor and planning notifications"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services to read and respond to notifications about planning/building cases. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Motta nabo- og planvarsel"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester til å lese og svare på varsel om plan-/byggesak. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Kultur og frivillighet

    /// <summary>
    /// Represents the 'Kunst og underholdning' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 75fa5863-3368-4ac6-9a4b-48f595e483ad
    /// - <c>URN:</c> urn:altinn:accesspackage:kunst-og-underholdning
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til kunstnerisk og underholdningsaktiviteter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> ArtsAndEntertainment { get; } = new ConstantDefinition<Package>("75fa5863-3368-4ac6-9a4b-48f595e483ad")
    {
        Entity = new()
        {
            Name = "Kunst og underholdning",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til kunstnerisk og underholdningsaktiviteter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:kunst-og-underholdning",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.CultureAndVolunteering,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Arts and entertainment"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to artistic and entertainment activities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kunst og underhaldning"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til kunstnarisk og underhaldningsaktivitetar. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Fornøyelser' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> c82ab275-dad6-461a-bf0c-50be46b25ec9
    /// - <c>URN:</c> urn:altinn:accesspackage:fornoyelser
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til drift av fornøyelsesetablissementer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Entertainment { get; } = new ConstantDefinition<Package>("c82ab275-dad6-461a-bf0c-50be46b25ec9")
    {
        Entity = new()
        {
            Name = "Fornøyelser",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til drift av fornøyelsesetablissementer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:fornoyelser",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.CultureAndVolunteering,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Amusements"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to the operation of amusement establishments. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Fornøyingar"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til drift av fornøyingsetablissementer. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Biblioteker, museer, arkiver og annen kultur' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 73418c0c-d4db-4e26-8581-4ccf1384aad7
    /// - <c>URN:</c> urn:altinn:accesspackage:biblioteker-museer-arkiver-og-annen-kultur
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til biblioteker, museer, arkiver, og annen kultur som botaniske og zoologiske hager, og drift av naturfenomener av historisk, kulturell eller undervisningsmessig interesse (f.eks. verdenskulturarv mv.). Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> LibrariesMuseumsArchivesOtherCulture { get; } = new ConstantDefinition<Package>("73418c0c-d4db-4e26-8581-4ccf1384aad7")
    {
        Entity = new()
        {
            Name = "Biblioteker, museer, arkiver og annen kultur",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til biblioteker, museer, arkiver, og annen kultur som botaniske og zoologiske hager, og drift av naturfenomener av historisk, kulturell eller undervisningsmessig interesse (f.eks. verdenskulturarv mv.). Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:biblioteker-museer-arkiver-og-annen-kultur",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.CultureAndVolunteering,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Libraries, museums, archives and other culture"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to libraries, museums, archives, and other culture such as botanical and zoological gardens, and operation of natural phenomena of historical, cultural or educational interest (e.g., world heritage sites, etc.). In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Bibliotek, museum, arkiv og anna kultur"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til bibliotek, museum, arkiv, og anna kultur som botaniske og zoologiske hagar, og drift av naturfenomen av historisk, kulturell eller undervisningsmessig interesse (t.d. verdskulturarv mv.). Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Lotteri og spill' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> cc5ccdec-6c67-4462-ab51-4d5eaafd64c1
    /// - <c>URN:</c> urn:altinn:accesspackage:lotteri-og-spill
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til lotteri og spill, som f eks kasinoer, bingohaller og videospillhaller samt spillevirksomhet som f.eks. lotterier og veddemål som inngås utenfor banen. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> LotteryAndGames { get; } = new ConstantDefinition<Package>("cc5ccdec-6c67-4462-ab51-4d5eaafd64c1")
    {
        Entity = new()
        {
            Name = "Lotteri og spill",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til lotteri og spill, som f eks kasinoer, bingohaller og videospillhaller samt spillevirksomhet som f.eks. lotterier og veddemål som inngås utenfor banen. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:lotteri-og-spill",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.CultureAndVolunteering,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Lottery and games"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to lottery and games, such as casinos, bingo halls and video game arcades, as well as gaming activities such as lotteries and betting entered into off-track. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Lotteri og spel"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til lotteri og spel, som til dømes kasinoar, bingohaller og videospelhaller samt spelverksemd som til dømes lotteri og veddemål som blir inngått utanfor banen. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Politikk' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> f05153b5-4784-46f0-805a-525ed31fde3b
    /// - <c>URN:</c> urn:altinn:accesspackage:politikk
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til aktiviteter i forbindelse med politisk arbeid. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Politics { get; } = new ConstantDefinition<Package>("f05153b5-4784-46f0-805a-525ed31fde3b")
    {
        Entity = new()
        {
            Name = "Politikk",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til aktiviteter i forbindelse med politisk arbeid. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:politikk",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.CultureAndVolunteering,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Politics"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to activities in connection with political work. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Politikk"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til aktivitetar i samband med politisk arbeid. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Sport og fritid' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> d2d00311-cc33-47ad-b33b-4eb15cce8d1d
    /// - <c>URN:</c> urn:altinn:accesspackage:sport-og-fritid
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til sports- og fritidsaktiviteter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> SportsAndRecreation { get; } = new ConstantDefinition<Package>("d2d00311-cc33-47ad-b33b-4eb15cce8d1d")
    {
        Entity = new()
        {
            Name = "Sport og fritid",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sports- og fritidsaktiviteter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:sport-og-fritid",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.CultureAndVolunteering,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Sports and recreation"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to sports and recreational activities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Sport og fritid"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til sports- og fritidsaktivitetar. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Energi, vann, avløp og avfall

    /// <summary>
    /// Represents the 'Miljørydding - rensing og lignende virksomhet' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ab656cf2-1e65-4b5a-a1e3-cd5bd4cb804c
    /// - <c>URN:</c> urn:altinn:accesspackage:miljorydding-rensing
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til miljøryddng, -rensing og lignende virksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> AccountantPayroll { get; } = new ConstantDefinition<Package>("ab656cf2-1e65-4b5a-a1e3-cd5bd4cb804c")
    {
        Entity = new()
        {
            Name = "Miljørydding - rensing og lignende virksomhet",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til miljøryddng, -rensing og lignende virksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:miljorydding-rensing",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnergyWaterSewageAndWaste,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Environmental remediation, cleaning and similar activities"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to environmental remediation, cleaning and similar activities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Miljørydding - reinsing og liknande verksemd"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til miljørydding, -reinsing og liknande verksemd. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Elektrisitet - produsere, overføre og distribuere' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> f56bdb15-686b-46f8-9343-bde5d7c17648
    /// - <c>URN:</c> urn:altinn:accesspackage:elektrisitet-produsere-overfore-distrubere
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til elektrisitet: produsere, overføre og distribuere. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> ElectricityProductionTransmissionDistribution { get; } = new ConstantDefinition<Package>("f56bdb15-686b-46f8-9343-bde5d7c17648")
    {
        Entity = new()
        {
            Name = "Elektrisitet - produsere, overføre og distribuere",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til elektrisitet: produsere, overføre og distribuere. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:elektrisitet-produsere-overfore-distrubere",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnergyWaterSewageAndWaste,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Electricity - production, transmission and distribution"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to electricity: production, transmission and distribution. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Elektrisitet - produsere, overføre og distribuere"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til elektrisitet: produsere, overføre og distribuere. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Utvinning av råolje, naturgass og kull' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 452bd15d-2cd2-4279-9470-cd97ba8ef1c7
    /// - <c>URN:</c> urn:altinn:accesspackage:utvinning-raaolje-naturgass-kull
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til utvinning av råolje, naturgass og kull. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> OilNaturalGasCoalExtraction { get; } = new ConstantDefinition<Package>("452bd15d-2cd2-4279-9470-cd97ba8ef1c7")
    {
        Entity = new()
        {
            Name = "Utvinning av råolje, naturgass og kull",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til utvinning av råolje, naturgass og kull. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:utvinning-raaolje-naturgass-kull",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnergyWaterSewageAndWaste,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Extraction of crude oil, natural gas and coal"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to the extraction of crude oil, natural gas and coal. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Utvinning av råolje, naturgass og kol"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til utvinning av råolje, naturgass og kol. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Samle opp og behandle avløpsvann' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> fef4aac0-d227-4ef6-834b-cc2eb4b942ed
    /// - <c>URN:</c> urn:altinn:accesspackage:samle-behandle-avlopsvann
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til samle opp og behandle avløpsvann. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> SewageCollectionTreatment { get; } = new ConstantDefinition<Package>("fef4aac0-d227-4ef6-834b-cc2eb4b942ed")
    {
        Entity = new()
        {
            Name = "Samle opp og behandle avløpsvann",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til samle opp og behandle avløpsvann. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:samle-behandle-avlopsvann",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnergyWaterSewageAndWaste,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Sewage collection and treatment"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to sewage collection and treatment. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Samle opp og behandle avløpsvatn"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til samle opp og behandle avløpsvatn. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Damp- og varmtvann' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 27afe398-b25b-4287-b0fa-c1d03d6c9fa9
    /// - <c>URN:</c> urn:altinn:accesspackage:damp-varmtvann
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til damp- og varmtvann. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> SteamAndHotWater { get; } = new ConstantDefinition<Package>("27afe398-b25b-4287-b0fa-c1d03d6c9fa9")
    {
        Entity = new()
        {
            Name = "Damp- og varmtvann",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til damp- og varmtvann. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:damp-varmtvann",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnergyWaterSewageAndWaste,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Steam and hot water"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to steam and hot water. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Damp- og varmvatn"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til damp- og varmvatn. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Avfall - samle inn, behandle, bruke og gjenvinne' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 6dcffdde-cc5c-4b57-a5a2-cc5ae6ad44fc
    /// - <c>URN:</c> urn:altinn:accesspackage:avfall-behandle-gjenvinne
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til avfall: samle inn, behandle bruke og gjenvinne. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> WasteCollectionTreatmentRecycling { get; } = new ConstantDefinition<Package>("6dcffdde-cc5c-4b57-a5a2-cc5ae6ad44fc")
    {
        Entity = new()
        {
            Name = "Avfall - samle inn, behandle, bruke og gjenvinne",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til avfall: samle inn, behandle bruke og gjenvinne. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:avfall-behandle-gjenvinne",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnergyWaterSewageAndWaste,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Waste - collection, treatment, use and recycling"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to waste: collection, treatment, use and recycling. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Avfall - samle inn, behandle, bruke og gjenvinne"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til avfall: samle inn, behandle, bruke og gjenvinne. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Vann - ta ut fra kilde, rense og distribuere' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> e7cda008-f265-4452-b03c-c21b8b51dfe1
    /// - <c>URN:</c> urn:altinn:accesspackage:vann-kilde-rense-distrubere
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til vann: ta ut fra kilde, rense og distribuere. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> WaterSourceTreatmentDistribution { get; } = new ConstantDefinition<Package>("e7cda008-f265-4452-b03c-c21b8b51dfe1")
    {
        Entity = new()
        {
            Name = "Vann - ta ut fra kilde, rense og distribuere",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til vann: ta ut fra kilde, rense og distribuere. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:vann-kilde-rense-distrubere",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnergyWaterSewageAndWaste,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Water - source extraction, treatment and distribution"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to water: source extraction, treatment and distribution. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Vatn - ta ut frå kjelde, reinske og distribuere"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til vatn: ta ut frå kjelde, reinske og distribuere. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Miljø, ulykke og sikkerhet

    /// <summary>
    /// Represents the 'Ulykke' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> a03af7d5-74b9-4f18-aead-5d47edc36be5
    /// - <c>URN:</c> urn:altinn:accesspackage:ulykke
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til ulykke. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Accident { get; } = new ConstantDefinition<Package>("a03af7d5-74b9-4f18-aead-5d47edc36be5")
    {
        Entity = new()
        {
            Name = "Ulykke",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til ulykke. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:ulykke",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnvironmentAccidentAndSafety,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Accident"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to accidents. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Ulykke"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til ulykke. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Miljørydding, miljørensing og lignende' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 04c5a001-5249-4765-ae8e-58617c404223
    /// - <c>URN:</c> urn:altinn:accesspackage:miljorydding-miljorensing-og-lignende
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til miljørydding, miljørensing og lignende. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> EnvironmentalCleanup { get; } = new ConstantDefinition<Package>("04c5a001-5249-4765-ae8e-58617c404223")
    {
        Entity = new()
        {
            Name = "Miljørydding, miljørensing og lignende",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til miljørydding, miljørensing og lignende. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:miljorydding-miljorensing-og-lignende",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnvironmentAccidentAndSafety,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Environmental cleanup, environmental remediation and similar"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to environmental cleanup, environmental remediation and similar activities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Miljørydding, miljøreinsing og liknande"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til miljørydding, miljøreinsing og liknande. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Yrkesskade' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> fa84bffc-ac17-40cd-af9c-61c89f92e44c
    /// - <c>URN:</c> urn:altinn:accesspackage:yrkesskade
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til yrkesskade. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> OccupationalInjury { get; } = new ConstantDefinition<Package>("fa84bffc-ac17-40cd-af9c-61c89f92e44c")
    {
        Entity = new()
        {
            Name = "Yrkesskade",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til yrkesskade. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:yrkesskade",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnvironmentAccidentAndSafety,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Occupational injury"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to occupational injuries. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Yrkesskade"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til yrkesskade. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Sikkerhet og internkontroll' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> cfe074fa-0a66-4a4b-974a-5d1db8eb94e6
    /// - <c>URN:</c> urn:altinn:accesspackage:sikkerhet-og-internkontroll
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til sikkerhet og internkontroll. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> SecurityAndInternalControl { get; } = new ConstantDefinition<Package>("cfe074fa-0a66-4a4b-974a-5d1db8eb94e6")
    {
        Entity = new()
        {
            Name = "Sikkerhet og internkontroll",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sikkerhet og internkontroll. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:sikkerhet-og-internkontroll",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnvironmentAccidentAndSafety,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Security and internal control"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to security and internal control. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tryggleik og internkontroll"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til tryggleik og internkontroll. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Bærekraft' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> bacc9294-56fd-457f-930e-59ee4a7a3894
    /// - <c>URN:</c> urn:altinn:accesspackage:baerekraft
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til tiltak og rapportering på bærekraft. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Sustainability { get; } = new ConstantDefinition<Package>("bacc9294-56fd-457f-930e-59ee4a7a3894")
    {
        Entity = new()
        {
            Name = "Bærekraft",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til tiltak og rapportering på bærekraft. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:baerekraft",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnvironmentAccidentAndSafety,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Sustainability"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to sustainability measures and reporting. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Berekraft"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til tiltak og rapportering på berekraft. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Renovasjon' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 5eb07bdc-5c3c-4c85-add3-5405b214b8a3
    /// - <c>URN:</c> urn:altinn:accesspackage:renovasjon
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til renovasjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> WasteManagement { get; } = new ConstantDefinition<Package>("5eb07bdc-5c3c-4c85-add3-5405b214b8a3")
    {
        Entity = new()
        {
            Name = "Renovasjon",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til renovasjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:renovasjon",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.EnvironmentAccidentAndSafety,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Waste management"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to waste management. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Renovasjon"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til renovasjon. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Helse, pleie, omsorg og vern

    /// <summary>
    /// Represents the 'Pleie- og omsorgstjenester i institusjon' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4d71e7b8-c6eb-4e33-9a64-1279a509a53b
    /// - <c>URN:</c> urn:altinn:accesspackage:pleie-omsorgstjenester-i-institusjon
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til pleie og omsorgstilbud i institursjon. Dette er tjenester som tilbyr institusjonsopphold kombinert med sykepleie, tilsyn eller annen form for pleie alt etter hva som kreves av beboerne. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> CareServicesInstitution { get; } = new ConstantDefinition<Package>("4d71e7b8-c6eb-4e33-9a64-1279a509a53b")
    {
        Entity = new()
        {
            Name = "Pleie- og omsorgstjenester i institusjon",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til pleie og omsorgstilbud i institursjon. Dette er tjenester som tilbyr institusjonsopphold kombinert med sykepleie, tilsyn eller annen form for pleie alt etter hva som kreves av beboerne. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:pleie-omsorgstjenester-i-institusjon",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.HealthCareAndProtection,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Care services in institutions"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to care services in institutions. These are services that offer institutional stays combined with nursing, supervision or other forms of care depending on what is required by the residents. This authorization may give the user access to health information about persons being reported on. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Pleie- og omsorgstenester i institusjon"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til pleie og omsorgstilbod i institusjon. Dette er tenester som tilbyr institusjonsopphald kombinert med sjukepleie, tilsyn eller anna form for pleie alt etter kva som blir kravd av bebuarane. Denne fullmakta kan gi brukar tilgang til helseopplysningar om personar det blir rapportert om. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Barnevern' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 83ff0734-0de5-4c2a-939b-18a9452c00bc
    /// - <c>URN:</c> urn:altinn:accesspackage:barnevern
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til barnevern. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. OBS! Vær oppmerksom på at tilgangspakken inneholder fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal gis som enkeltrettigheter.
    /// </remarks>
    public static ConstantDefinition<Package> ChildProtection { get; } = new ConstantDefinition<Package>("83ff0734-0de5-4c2a-939b-18a9452c00bc")
    {
        Entity = new()
        {
            Name = "Barnevern",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til barnevern. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. OBS! Vær oppmerksom på at tilgangspakken inneholder fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal gis som enkeltrettigheter.",
            Urn = "urn:altinn:accesspackage:barnevern",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.HealthCareAndProtection,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Child protection services"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to child protection services. This authorization may give the user access to health information about persons being reported on. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides. NOTE! Be aware that the access package contains authorizations that may be of a sensitive nature. Consider whether the authorizations should be granted as individual rights.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Barnevern"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til barnevern. Denne fullmakta kan gi brukar tilgang til helseopplysningar om personar det blir rapportert om. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir. OBS! Ver merksam på at tilgangspakken inneheld fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal bli gitt som enkeltrettar.")
        ),
    };

    /// <summary>
    /// Represents the 'Familievern' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 98c404f4-5350-42cd-86d0-15fd38f178c4
    /// - <c>URN:</c> urn:altinn:accesspackage:familievern
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til familievern. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. OBS! Vær oppmerksom på at tilgangspakken inneholder fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal gis som enkeltrettigheter.
    /// </remarks>
    public static ConstantDefinition<Package> FamilyCounseling { get; } = new ConstantDefinition<Package>("98c404f4-5350-42cd-86d0-15fd38f178c4")
    {
        Entity = new()
        {
            Name = "Familievern",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til familievern. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. OBS! Vær oppmerksom på at tilgangspakken inneholder fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal gis som enkeltrettigheter.",
            Urn = "urn:altinn:accesspackage:familievern",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.HealthCareAndProtection,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Family counseling"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to family counseling. This authorization may give the user access to health information about persons being reported on. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides. NOTE! Be aware that the access package contains authorizations that may be of a sensitive nature. Consider whether the authorizations should be granted as individual rights.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Familievern"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til familievern. Denne fullmakta kan gi brukar tilgang til helseopplysningar om personar det blir rapportert om. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir. OBS! Ver merksam på at tilgangspakken inneheld fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal bli gitt som enkeltrettar.")
        ),
    };

    /// <summary>
    /// Represents the 'Helsetjenester' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4fa0fbbc-3841-4405-9d94-12731a8fdb81
    /// - <c>URN:</c> urn:altinn:accesspackage:helsetjenester
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til sykehus, lege, tannlege og hjemmesykepleie,fysioterapi, ambulanse og lignende. Denne fullmakten kan gi bruker tilgang til personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> HealthServices { get; } = new ConstantDefinition<Package>("4fa0fbbc-3841-4405-9d94-12731a8fdb81")
    {
        Entity = new()
        {
            Name = "Helsetjenester",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sykehus, lege, tannlege og hjemmesykepleie,fysioterapi, ambulanse og lignende. Denne fullmakten kan gi bruker tilgang til personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:helsetjenester",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.HealthCareAndProtection,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Health services"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to hospitals, doctors, dentists, home nursing, physiotherapy, ambulances and similar services. This authorization may give the user access to personal information about persons being reported on. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Helsetenester"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til sjukehus, lege, tannlege og heimsjukepleie, fysioterapi, ambulanse og liknande. Denne fullmakta kan gi brukar tilgang til personopplysningar om personar det blir rapportert om. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Helsetjenester med personopplysninger av særlig kategori' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 5a642040-46cf-4466-b671-115a022e3048
    /// - <c>URN:</c> urn:altinn:accesspackage:helsetjenester-personopplysninger-saerlig-kategori
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til sykehus, lege, tannlege og hjemmesykepleie,fysioterapi, ambulanse og lignende, som er av særlig kategori. Denne fullmakten kan gi bruker tilgang til sensitive personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> HealthServicesSpecialCategory { get; } = new ConstantDefinition<Package>("5a642040-46cf-4466-b671-115a022e3048")
    {
        Entity = new()
        {
            Name = "Helsetjenester med personopplysninger av særlig kategori",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sykehus, lege, tannlege og hjemmesykepleie,fysioterapi, ambulanse og lignende, som er av særlig kategori. Denne fullmakten kan gi bruker tilgang til sensitive personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:helsetjenester-personopplysninger-saerlig-kategori",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.HealthCareAndProtection,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Health services with personal data of special category"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to hospitals, doctors, dentists, home nursing, physiotherapy, ambulances and similar services, which are of a special category. This authorization may give the user access to sensitive personal information about persons being reported on. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Helsetenester med personopplysningar av særleg kategori"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til sjukehus, lege, tannlege og heimsjukepleie, fysioterapi, ambulanse og liknande, som er av særleg kategori. Denne fullmakta kan gi brukar tilgang til sensitive personopplysningar om personar det blir rapportert om. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Kommuneoverlege' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 03404270-aa2d-498f-9e6a-103043d41f1f
    /// - <c>URN:</c> urn:altinn:accesspackage:kommuneoverlege
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til sykehus, lege, tannlege og hjemmesykepleie,fysioterapi, ambulanse og lignende, som er relevant for kommuneleger. Denne fullmakten kan gi bruker tilgang til personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> MunicipalDoctor { get; } = new ConstantDefinition<Package>("03404270-aa2d-498f-9e6a-103043d41f1f")
    {
        Entity = new()
        {
            Name = "Kommuneoverlege",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sykehus, lege, tannlege og hjemmesykepleie,fysioterapi, ambulanse og lignende, som er relevant for kommuneleger. Denne fullmakten kan gi bruker tilgang til personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:kommuneoverlege",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.HealthCareAndProtection,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Municipal chief medical officer"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to hospitals, doctors, dentists, home nursing, physiotherapy, ambulances and similar services, which are relevant for municipal doctors. This authorization may give the user access to personal information about persons being reported on. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kommuneoverlege"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til sjukehus, lege, tannlege og heimsjukepleie, fysioterapi, ambulanse og liknande, som er relevant for kommunelegar. Denne fullmakta kan gi brukar tilgang til personopplysningar om personar det blir rapportert om. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Sosiale omsorgstjenester uten botilbud og flyktningemottak' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 70831546-6bfa-45a0-bdad-14e9db265847
    /// - <c>URN:</c> urn:altinn:accesspackage:sosiale-omsorgstjenester-uten-botilbud-og-flyktningemottak
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til sosiale omsorgstjeneser uten botilbud for eldre, funksjonshemmede og rusmisbrukere samt flykningemottak, og tjenester relatert til arbeidstrening og andre sosiale tjenester, f eks i regi av velferdsorganisasjoner. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> SocialCareServicesWithoutHousing { get; } = new ConstantDefinition<Package>("70831546-6bfa-45a0-bdad-14e9db265847")
    {
        Entity = new()
        {
            Name = "Sosiale omsorgstjenester uten botilbud og flyktningemottak",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sosiale omsorgstjeneser uten botilbud for eldre, funksjonshemmede og rusmisbrukere samt flykningemottak, og tjenester relatert til arbeidstrening og andre sosiale tjenester, f eks i regi av velferdsorganisasjoner. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:sosiale-omsorgstjenester-uten-botilbud-og-flyktningemottak",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.HealthCareAndProtection,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Social care services without housing and refugee reception"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to social care services without housing for the elderly, disabled and substance abusers as well as refugee reception centers, and services related to work training and other social services, e.g., under the auspices of welfare organizations. This authorization may give the user access to health information about persons being reported on. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Sosiale omsorgstenester utan butilbod og flyktningemottak"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til sosiale omsorgstenester utan butilbod for eldre, funksjonshemma og rusmisbrukarar samt flyktningemottak, og tenester relatert til arbeidstrening og andre sosiale tenester, til dømes i regi av velferdsorganisasjonar. Denne fullmakta kan gi brukar tilgang til helseopplysningar om personar det blir rapportert om. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Industrier

    /// <summary>
    /// Represents the 'Næringsmidler, drikkevarer og tobakk' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 65c7bbe7-aeb4-4f18-b0b0-1b1b83bd24d1
    /// - <c>URN:</c> urn:altinn:accesspackage:naeringsmidler-drikkevarer-og-tobakk
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med næringsmidler, drikkevarer og tobakk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> FoodBeveragesTobacco { get; } = new ConstantDefinition<Package>("65c7bbe7-aeb4-4f18-b0b0-1b1b83bd24d1")
    {
        Entity = new()
        {
            Name = "Næringsmidler, drikkevarer og tobakk",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med næringsmidler, drikkevarer og tobakk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:naeringsmidler-drikkevarer-og-tobakk",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Food, beverages and tobacco"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with food, beverages and tobacco. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Næringsmiddel, drikkevarar og tobakk"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med næringsmiddel, drikkevarar og tobakk. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Møbler og annen industri' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 07bfd8a5-5b13-4937-84b9-2bd6ac726ea1
    /// - <c>URN:</c> urn:altinn:accesspackage:mobler-og-annen-industri
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med møbler og annen industri. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> FurnitureOtherManufacturing { get; } = new ConstantDefinition<Package>("07bfd8a5-5b13-4937-84b9-2bd6ac726ea1")
    {
        Entity = new()
        {
            Name = "Møbler og annen industri",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med møbler og annen industri. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:mobler-og-annen-industri",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Furniture and other manufacturing"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with furniture and other manufacturing. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Møblar og anna industri"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med møblar og anna industri. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Trelast, trevarer og papirvarer' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> deb1618f-395c-4a14-9a70-20e90e5f9a76
    /// - <c>URN:</c> urn:altinn:accesspackage:trelast-trevarer-papirvarer
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med trelast, trevarer og papirvarer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> LumberWoodProductsPaperGoods { get; } = new ConstantDefinition<Package>("deb1618f-395c-4a14-9a70-20e90e5f9a76")
    {
        Entity = new()
        {
            Name = "Trelast, trevarer og papirvarer",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med trelast, trevarer og papirvarer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:trelast-trevarer-papirvarer",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Lumber, wood products and paper goods"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with lumber, wood products and paper goods. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Trelast, trevarar og papirvarar"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med trelast, trevarar og papirvarar. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Metallvarer elektrisk utstyr og maskiner' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 2857fd90-dad2-4cc3-9947-282c22f5d2dc
    /// - <c>URN:</c> urn:altinn:accesspackage:metallvarer-elektrisk-utstyr-og-maskiner
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med metallvarer, elektrisk utstyr og maskiner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> MetalProductsElectricalEquipmentMachinery { get; } = new ConstantDefinition<Package>("2857fd90-dad2-4cc3-9947-282c22f5d2dc")
    {
        Entity = new()
        {
            Name = "Metallvarer elektrisk utstyr og maskiner",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med metallvarer, elektrisk utstyr og maskiner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:metallvarer-elektrisk-utstyr-og-maskiner",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Metal products, electrical equipment and machinery"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with metal products, electrical equipment and machinery. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Metallvarar, elektrisk utstyr og maskiner"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med metallvarar, elektrisk utstyr og maskiner. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Metaller og mineraler' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 84e744a0-e93f-4caf-bb3f-24387f045d2d
    /// - <c>URN:</c> urn:altinn:accesspackage:metaller-og-mineraler
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med metaller og mineraler. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> MetalsAndMinerals { get; } = new ConstantDefinition<Package>("84e744a0-e93f-4caf-bb3f-24387f045d2d")
    {
        Entity = new()
        {
            Name = "Metaller og mineraler",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med metaller og mineraler. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:metaller-og-mineraler",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Metals and minerals"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with metals and minerals. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Metallar og mineral"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med metallar og mineral. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Bergverk' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 8c700369-8fff-40f1-b4ce-2f116416d804
    /// - <c>URN:</c> urn:altinn:accesspackage:bergverk
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med bergverk og tilhørende tjenester til bergverksdrift og utvinning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Mining { get; } = new ConstantDefinition<Package>("8c700369-8fff-40f1-b4ce-2f116416d804")
    {
        Entity = new()
        {
            Name = "Bergverk",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med bergverk og tilhørende tjenester til bergverksdrift og utvinning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:bergverk",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Mining"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with mining and associated services for mining operations and extraction. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Bergverk"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med bergverk og tilhøyrande tenester til bergverksdrift og utvinning. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Oljeraffinering, kjemisk og farmasøytisk industri' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 79203df9-d71b-460b-a828-22b0bf79f335
    /// - <c>URN:</c> urn:altinn:accesspackage:oljeraffinering-kjemisk-farmasoytisk-industri
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med oljeraffinering, kjemisk og farmasøytisk industri. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> OilRefiningChemicalPharmaceuticalIndustry { get; } = new ConstantDefinition<Package>("79203df9-d71b-460b-a828-22b0bf79f335")
    {
        Entity = new()
        {
            Name = "Oljeraffinering, kjemisk og farmasøytisk industri",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med oljeraffinering, kjemisk og farmasøytisk industri. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:oljeraffinering-kjemisk-farmasoytisk-industri",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Oil refining, chemical and pharmaceutical industry"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with oil refining, chemical and pharmaceutical industries. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Oljeraffinering, kjemisk og farmasøytisk industri"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med oljeraffinering, kjemisk og farmasøytisk industri. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Trykkerier og reproduksjon av innspilte opptak' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 965f9aca-48f5-4a16-b5e3-228806ad4fa7
    /// - <c>URN:</c> urn:altinn:accesspackage:trykkerier-reproduksjon-opptak
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med trykkerier og reproduksjon av innspilte opptak. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> PrintingRecordedMediaReproduction { get; } = new ConstantDefinition<Package>("965f9aca-48f5-4a16-b5e3-228806ad4fa7")
    {
        Entity = new()
        {
            Name = "Trykkerier og reproduksjon av innspilte opptak",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med trykkerier og reproduksjon av innspilte opptak. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:trykkerier-reproduksjon-opptak",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Printing and reproduction of recorded media"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with printing and reproduction of recorded media. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Trykkeri og reproduksjon av innspelte opptak"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med trykkeri og reproduksjon av innspelte opptak. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Reparasjon og installasjon av maskiner og utstyr' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ec492675-3a48-4ad9-b864-2d6865020642
    /// - <c>URN:</c> urn:altinn:accesspackage:reparasjon-og-installasjon-av-maskiner-og-utstyr
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med reparasjon og installasjon av maskiner og utstyr. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> ResponsibleAuditor { get; } = new ConstantDefinition<Package>("ec492675-3a48-4ad9-b864-2d6865020642")
    {
        Entity = new()
        {
            Name = "Reparasjon og installasjon av maskiner og utstyr",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med reparasjon og installasjon av maskiner og utstyr. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:reparasjon-og-installasjon-av-maskiner-og-utstyr",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Repair and installation of machinery and equipment"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with repair and installation of machinery and equipment. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Reparasjon og installasjon av maskiner og utstyr"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med reparasjon og installasjon av maskiner og utstyr. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Gummi, plast og ikke-metallholdige mineralprodukter' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 573be5d2-3ddd-4862-9711-2350147a1b25
    /// - <c>URN:</c> urn:altinn:accesspackage:gummi-plast-og-ikkemetallholdige-mineralprodukter
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med gummi, plast og ikke-metallholdige mineralprodukter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> RubberPlasticNonMetallicMineralProducts { get; } = new ConstantDefinition<Package>("573be5d2-3ddd-4862-9711-2350147a1b25")
    {
        Entity = new()
        {
            Name = "Gummi, plast og ikke-metallholdige mineralprodukter",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med gummi, plast og ikke-metallholdige mineralprodukter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:gummi-plast-og-ikkemetallholdige-mineralprodukter",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Rubber, plastic and non-metallic mineral products"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with rubber, plastic and non-metallic mineral products. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Gummi, plast og ikkje-metallhaldige mineralprodukt"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med gummi, plast og ikkje-metallhaldige mineralprodukt. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Verft og andre transportmidler' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> c37aabdf-a78a-4f59-999a-298a83e9e113
    /// - <c>URN:</c> urn:altinn:accesspackage:verft-og-andre-transportmidler
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med verft og andre transportmidler. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> ShipyardsOtherTransportVehicles { get; } = new ConstantDefinition<Package>("c37aabdf-a78a-4f59-999a-298a83e9e113")
    {
        Entity = new()
        {
            Name = "Verft og andre transportmidler",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med verft og andre transportmidler. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:verft-og-andre-transportmidler",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Shipyards and other transport vehicles"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with shipyards and other transport vehicles. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Verft og andre transportmiddel"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med verft og andre transportmiddel. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Tekstiler, klær og lærvarer' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> c6231d60-5373-4179-b98b-1e7eb83da474
    /// - <c>URN:</c> urn:altinn:accesspackage:tekstiler-klaer-laervarer
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med tekstiler, klær og lærvarer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> TextilesClothingLeatherGoods { get; } = new ConstantDefinition<Package>("c6231d60-5373-4179-b98b-1e7eb83da474")
    {
        Entity = new()
        {
            Name = "Tekstiler, klær og lærvarer",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med tekstiler, klær og lærvarer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:tekstiler-klaer-laervarer",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Industries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Textiles, clothing and leather goods"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to industries in connection with textiles, clothing and leather goods. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tekstilar, klede og lærvarar"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til industri i samband med tekstilar, klede og lærvarar. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Integrasjoner

    /// <summary>
    /// Represents the 'Delegerbare Maskinporten scopes' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 27f488d7-d1f6-4aee-ae81-2bb42d62c446
    /// - <c>URN:</c> urn:altinn:accesspackage:maskinporten-scopes
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til data og programmeringsgrensenitt (API) som benytter Maskinporten eller tilsvarende løsninger for APIsikring. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> DelegableMaskinportenScopes { get; } = new ConstantDefinition<Package>("27f488d7-d1f6-4aee-ae81-2bb42d62c446")
    {
        Entity = new()
        {
            Name = "Delegerbare Maskinporten scopes",
            Description = "Denne tilgangspakken gir fullmakter til data og programmeringsgrensenitt (API) som benytter Maskinporten eller tilsvarende løsninger for APIsikring. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:maskinporten-scopes",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Integrations,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Delegable maskinporten scopes"),
            KeyValuePair.Create("Description", "This access package provides authorizations for data and application programming interfaces (APIs) that use Maskinporten or equivalent solutions for API security. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Delegerbare Maskinporten scopes"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til data og programmeringsgrensesnitt (API) som nyttar Maskinporten eller tilsvarande løysingar for API-sikring. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Delegerbare Maskinporten scopes - NUF' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 5dad616e-5538-4e3f-b15a-bae33f06c99f
    /// - <c>URN:</c> urn:altinn:accesspackage:maskinporten-scopes-nuf
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til data og programmeringsgrensenitt (API) som benytter Maskinporten eller tilsvarende løsninger for APIsikring på vegne av norskregistrerte utenlandske foretak (NUF). Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> DelegableMaskinportenScopesNUF { get; } = new ConstantDefinition<Package>("5dad616e-5538-4e3f-b15a-bae33f06c99f")
    {
        Entity = new()
        {
            Name = "Delegerbare Maskinporten scopes - NUF",
            Description = "Denne tilgangspakken gir fullmakter til data og programmeringsgrensenitt (API) som benytter Maskinporten eller tilsvarende løsninger for APIsikring på vegne av norskregistrerte utenlandske foretak (NUF). Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:maskinporten-scopes-nuf",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Integrations,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Delegable maskinporten scopes - NUF"),
            KeyValuePair.Create("Description", "This access package provides authorizations for data and application programming interfaces (APIs) that use Maskinporten or equivalent solutions for API security on behalf of Norwegian Registered Foreign Enterprises (NUF). In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Delegerbare Maskinporten scopes - NUF"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til data og programmeringsgrensesnitt (API) som nyttar Maskinporten eller tilsvarande løysingar for API-sikring på vegner av norskregistrerte utanlandske føretak (NUF). Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Maskinlesbare hendelser' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 66bd8101-fbd8-491b-b1f5-2f53a9476ffd
    /// - <c>URN:</c> urn:altinn:accesspackage:maskinlesbare-hendelser
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til å administrere tilgang til maskinlesbare hendelser. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> MachineReadableEventsIntegration { get; } = new ConstantDefinition<Package>("66bd8101-fbd8-491b-b1f5-2f53a9476ffd")
    {
        Entity = new()
        {
            Name = "Maskinlesbare hendelser",
            Description = "Denne tilgangspakken gir fullmakter til å administrere tilgang til maskinlesbare hendelser. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:maskinlesbare-hendelser",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Integrations,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Machine-readable events"),
            KeyValuePair.Create("Description", "This access package provides authorizations to administer access to machine-readable events. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Maskinlesbare hendingar"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til å administrere tilgang til maskinlesbare hendingar. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Post og arkiv

    /// <summary>
    /// Represents the 'Post til virksomheten med taushetsbelagt innhold' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> bb0569a6-2268-49b5-9d38-8158b26124c3
    /// - <c>URN:</c> urn:altinn:accesspackage:post-til-virksomheten-med-taushetsbelagt-innhold
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til all mottatt post som innholder sensitiv/taushetsbelagt informasjon som sendes virksomheten. Fullmakten gis normalt til de i virksomheten som håndterer mottak av post som har taushetsbelagt innhold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> ConfidentialMailToBusiness { get; } = new ConstantDefinition<Package>("bb0569a6-2268-49b5-9d38-8158b26124c3")
    {
        Entity = new()
        {
            Name = "Post til virksomheten med taushetsbelagt innhold",
            Description = "Denne fullmakten gir tilgang til all mottatt post som innholder sensitiv/taushetsbelagt informasjon som sendes virksomheten. Fullmakten gis normalt til de i virksomheten som håndterer mottak av post som har taushetsbelagt innhold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:post-til-virksomheten-med-taushetsbelagt-innhold",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.MailAndArchive,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Mail to business with confidential content"),
            KeyValuePair.Create("Description", "This authorization provides access to all received mail containing sensitive/confidential information sent to the business. The authorization is normally granted to those in the business who handle the receipt of mail with confidential content. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Post til verksemda med teiepliktbelagt innhald"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til all motteke post som inneheld sensitiv/teiepliktbelagt informasjon som blir send verksemda. Fullmakta blir normalt gitt til dei i verksemda som handterer mottak av post som har teiepliktbelagt innhald. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Ordinær post til virksomheten' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 91cf61ae-69ab-49d5-b51a-80591c91f255
    /// - <c>URN:</c> urn:altinn:accesspackage:ordinaer-post-til-virksomheten
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til all mottatt post som ikke innholder sensitiv/taushetsbelagt informasjon som sendes virksomheten. Fullmakten gis normalt til de i virksomheten som håndterer mottak av post. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> RegularMailToBusiness { get; } = new ConstantDefinition<Package>("91cf61ae-69ab-49d5-b51a-80591c91f255")
    {
        Entity = new()
        {
            Name = "Ordinær post til virksomheten",
            Description = "Denne fullmakten gir tilgang til all mottatt post som ikke innholder sensitiv/taushetsbelagt informasjon som sendes virksomheten. Fullmakten gis normalt til de i virksomheten som håndterer mottak av post. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:ordinaer-post-til-virksomheten",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.MailAndArchive,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Regular mail to business"),
            KeyValuePair.Create("Description", "This authorization provides access to all received mail that does not contain sensitive/confidential information sent to the business. The authorization is normally granted to those in the business who handle the receipt of mail. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Ordinær post til verksemda"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til all motteke post som ikkje inneheld sensitiv/teiepliktbelagt informasjon som blir send verksemda. Fullmakta blir normalt gitt til dei i verksemda som handterer mottak av post. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Administrere tilganger

    /// <summary>
    /// Represents the 'Tilgangsstyrer' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0195efb8-7c80-7a95-ad36-900c3d8ad300
    /// - <c>URN:</c> urn:altinn:accesspackage:tilgangsstyrer
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Gir mulighet til å gi videre tilganger for virksomheten som man selv har mottatt
    /// </remarks>
    public static ConstantDefinition<Package> AccessManager { get; } = new ConstantDefinition<Package>("0195efb8-7c80-7a95-ad36-900c3d8ad300")
    {
        Entity = new()
        {
            Name = "Tilgangsstyrer",
            Description = "Gir mulighet til å gi videre tilganger for virksomheten som man selv har mottatt",
            Urn = "urn:altinn:accesspackage:tilgangsstyrer",
            IsDelegable = false,
            HasResources = false,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ManageAccess,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access manager"),
            KeyValuePair.Create("Description", "Provides the ability to delegate access for the business that one has received oneself")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tilgangsstyrar"),
            KeyValuePair.Create("Description", "Gir moglegheit til å gi vidare tilgangar for verksemda som ein sjølv har motteke")
        ),
    };

    /// <summary>
    /// Represents the 'Klientadministrator' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0195efb8-7c80-7e82-9b4f-7d63e773bbca
    /// - <c>URN:</c> urn:altinn:accesspackage:klientadministrator
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Gir mulighet til å administrere tilgang til tjenester videre til ansatte på vegne av deres kunder
    /// </remarks>
    public static ConstantDefinition<Package> ClientAdministrator { get; } = new ConstantDefinition<Package>("0195efb8-7c80-7e82-9b4f-7d63e773bbca")
    {
        Entity = new()
        {
            Name = "Klientadministrator",
            Description = "Gir mulighet til å administrere tilgang til tjenester videre til ansatte på vegne av deres kunder",
            Urn = "urn:altinn:accesspackage:klientadministrator",
            IsDelegable = false,
            HasResources = false,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ManageAccess,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Client administrator"),
            KeyValuePair.Create("Description", "Provides the ability to administer access to services to employees on behalf of their clients")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Klientadministrator"),
            KeyValuePair.Create("Description", "Gir moglegheit til å administrere tilgang til tenester vidare til tilsette på vegner av deira kundar")
        ),
    };

    /// <summary>
    /// Represents the 'Konkursbo administrator' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0195efb8-7c80-7e9c-95c1-48937e23960a
    /// - <c>URN:</c> urn:altinn:accesspackage:konkursbo-tilgangsstyrer
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Gir bruker mulighet til å administrere konkursbo
    /// </remarks>
    public static ConstantDefinition<Package> KonkursboAdministrator { get; } = new ConstantDefinition<Package>("0195efb8-7c80-7e9c-95c1-48937e23960a")
    {
        Entity = new()
        {
            Name = "Konkursbo administrator",
            Description = "Gir bruker mulighet til å administrere konkursbo",
            Urn = "urn:altinn:accesspackage:konkursbo-tilgangsstyrer",
            IsDelegable = false,
            HasResources = false,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ManageAccess,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Bankruptcy estate administrator"),
            KeyValuePair.Create("Description", "Provides the user with the ability to administer bankruptcy estates")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Konkursbu administrator"),
            KeyValuePair.Create("Description", "Gir brukar moglegheit til å administrere konkursbu")
        ),
    };

    /// <summary>
    /// Represents the 'Hovedadministrator' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0195efb8-7c80-7e16-ab0c-36dc8ab1a29d
    /// - <c>URN:</c> urn:altinn:accesspackage:hovedadministrator
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Gir mulighet til å administrere alle tilganger for virksomheten
    /// </remarks>
    public static ConstantDefinition<Package> MainAdministrator { get; } = new ConstantDefinition<Package>("0195efb8-7c80-7e16-ab0c-36dc8ab1a29d")
    {
        Entity = new()
        {
            Name = "Hovedadministrator",
            Description = "Gir mulighet til å administrere alle tilganger for virksomheten",
            Urn = "urn:altinn:accesspackage:hovedadministrator",
            IsDelegable = false,
            HasResources = false,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ManageAccess,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Main administrator"),
            KeyValuePair.Create("Description", "Provides the ability to administer all access for the business")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Hovudadministrator"),
            KeyValuePair.Create("Description", "Gir moglegheit til å administrere alle tilgangar for verksemda")
        ),
    };

    /// <summary>
    /// Represents the 'Maskinporten administrator' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0195efb8-7c80-7b30-a84d-f37fed9fb89c
    /// - <c>URN:</c> urn:altinn:accesspackage:maskinporten-administrator
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Gir bruker mulighet til å administrere tilgang til maskinporten scopes
    /// </remarks>
    public static ConstantDefinition<Package> MaskinportenAdministrator { get; } = new ConstantDefinition<Package>("0195efb8-7c80-7b30-a84d-f37fed9fb89c")
    {
        Entity = new()
        {
            Name = "Maskinporten administrator",
            Description = "Gir bruker mulighet til å administrere tilgang til maskinporten scopes",
            Urn = "urn:altinn:accesspackage:maskinporten-administrator",
            IsDelegable = false,
            HasResources = false,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.ManageAccess,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Maskinporten administrator"),
            KeyValuePair.Create("Description", "Provides the user with the ability to administer access to Maskinporten scopes")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Maskinporten administrator"),
            KeyValuePair.Create("Description", "Gir brukar moglegheit til å administrere tilgang til Maskinporten scopes")
        ),
    };

    #endregion

    #region Andre tjenesteytende næringer

    /// <summary>
    /// Represents the 'Finansiering og forsikring' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 8f977b5f-a2f9-4712-88e2-ab1a51a6b26f
    /// - <c>URN:</c> urn:altinn:accesspackage:finansiering-og-forsikring
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester knyttet til finansiering og forsikring. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> FinancingAndInsurance { get; } = new ConstantDefinition<Package>("8f977b5f-a2f9-4712-88e2-ab1a51a6b26f")
    {
        Entity = new()
        {
            Name = "Finansiering og forsikring",
            Description = "Denne fullmakten gir tilgang til tjenester knyttet til finansiering og forsikring. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:finansiering-og-forsikring",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.OtherServiceIndustries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Financing and insurance"),
            KeyValuePair.Create("Description", "This authorization provides access to services related to financing and insurance. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Finansiering og forsikring"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester knytt til finansiering og forsikring. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Informasjon og kommunikasjon' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 413d79fb-a419-4e74-98f7-aa91389deb81
    /// - <c>URN:</c> urn:altinn:accesspackage:informasjon-og-kommunikasjon
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester knyttet til informasjon og kommunikasjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> InformationAndCommunication { get; } = new ConstantDefinition<Package>("413d79fb-a419-4e74-98f7-aa91389deb81")
    {
        Entity = new()
        {
            Name = "Informasjon og kommunikasjon",
            Description = "Denne fullmakten gir tilgang til tjenester knyttet til informasjon og kommunikasjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:informasjon-og-kommunikasjon",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.OtherServiceIndustries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Information and communication"),
            KeyValuePair.Create("Description", "This authorization provides access to services related to information and communication. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Informasjon og kommunikasjon"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester knytt til informasjon og kommunikasjon. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Annen tjenesteyting' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> a8b18888-2216-4eaa-972d-ae96da04b1ac
    /// - <c>URN:</c> urn:altinn:accesspackage:annen-tjenesteyting
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester knyttet til annen tjenesteyting som f eks organisasjoner og foreninger, reparasjon av datamaskiner og varer til personlig bruk og husholdningsbruk og en rekke personlige tjenester som ikke er nevnt annet sted. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> OtherServices { get; } = new ConstantDefinition<Package>("a8b18888-2216-4eaa-972d-ae96da04b1ac")
    {
        Entity = new()
        {
            Name = "Annen tjenesteyting",
            Description = "Denne fullmakten gir tilgang til tjenester knyttet til annen tjenesteyting som f eks organisasjoner og foreninger, reparasjon av datamaskiner og varer til personlig bruk og husholdningsbruk og en rekke personlige tjenester som ikke er nevnt annet sted. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:annen-tjenesteyting",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.OtherServiceIndustries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Other services"),
            KeyValuePair.Create("Description", "This authorization provides access to services related to other services such as organizations and associations, repair of computers and goods for personal and household use, and a range of personal services not mentioned elsewhere. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Anna tenesteyting"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester knytt til anna tenesteyting som til dømes organisasjonar og foreningar, reparasjon av datamaskiner og varer til personleg bruk og hushaldningsbruk og ei rekkje personlege tenester som ikkje er nemnt anna stad. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Post- og telekommunikasjon' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> a736c33b-c15a-43ac-85be-a684630e1e59
    /// - <c>URN:</c> urn:altinn:accesspackage:post-og-telekommunikasjon
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester knyttet til post og telekommunikasjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> PostalAndTelecommunications { get; } = new ConstantDefinition<Package>("a736c33b-c15a-43ac-85be-a684630e1e59")
    {
        Entity = new()
        {
            Name = "Post- og telekommunikasjon",
            Description = "Denne fullmakten gir tilgang til tjenester knyttet til post og telekommunikasjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:post-og-telekommunikasjon",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.OtherServiceIndustries,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Postal and telecommunications"),
            KeyValuePair.Create("Description", "This authorization provides access to services related to postal and telecommunications. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Post- og telekommunikasjon"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester knytt til post og telekommunikasjon. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Personale

    /// <summary>
    /// Represents the 'A-ordningen' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4af2d44d-f9f8-4585-b679-7875bb1828ea
    /// - <c>URN:</c> urn:altinn:accesspackage:a-ordning
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester som inngår i A-ordningen. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  OBS! Vær oppmerksompå at tilgangspakken inneholder fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal gis som enkeltrettigheter.
    /// </remarks>
    public static ConstantDefinition<Package> AOrderSystem { get; } = new ConstantDefinition<Package>("4af2d44d-f9f8-4585-b679-7875bb1828ea")
    {
        Entity = new()
        {
            Name = "A-ordningen",
            Description = "Denne tilgangspakken gir fullmakter til tjenester som inngår i A-ordningen. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  OBS! Vær oppmerksompå at tilgangspakken inneholder fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal gis som enkeltrettigheter.",
            Urn = "urn:altinn:accesspackage:a-ordning",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Personnel,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "A-melding system"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services included in the A-melding system. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides. NOTE! Be aware that the access package contains authorizations that may be of a sensitive nature. Consider whether the authorizations should be granted as individual rights.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "A-ordninga"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester som inngår i A-ordninga. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir. OBS! Ver merksam på at tilgangspakken inneheld fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal bli gitt som enkeltrettar.")
        ),
    };

    /// <summary>
    /// Represents the 'Ansettelsesforhold' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> cf575d42-72be-4d14-b0d2-77352221df4f
    /// - <c>URN:</c> urn:altinn:accesspackage:ansettelsesforhold
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til ansettelsesforhold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> EmploymentRelations { get; } = new ConstantDefinition<Package>("cf575d42-72be-4d14-b0d2-77352221df4f")
    {
        Entity = new()
        {
            Name = "Ansettelsesforhold",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til ansettelsesforhold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:ansettelsesforhold",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Personnel,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Employment relations"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to employment relations. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tilsetjingsforhold"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til tilsetjingsforhold. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Permisjon' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 71a2bd47-e885-49c7-8a58-7ef4bd936f41
    /// - <c>URN:</c> urn:altinn:accesspackage:permisjon
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til permisjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Leave { get; } = new ConstantDefinition<Package>("71a2bd47-e885-49c7-8a58-7ef4bd936f41")
    {
        Entity = new()
        {
            Name = "Permisjon",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til permisjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:permisjon",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Personnel,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Leave of absence"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to leave of absence. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Permisjon"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til permisjon. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Pensjon' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ae41bf08-c7ef-4bfe-b2b9-7bf6deb7798f
    /// - <c>URN:</c> urn:altinn:accesspackage:pensjon
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til pensjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Pension { get; } = new ConstantDefinition<Package>("ae41bf08-c7ef-4bfe-b2b9-7bf6deb7798f")
    {
        Entity = new()
        {
            Name = "Pensjon",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til pensjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:pensjon",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Personnel,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Pension"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to pensions. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Pensjon"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til pensjon. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Lønn' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> fb0aa257-e7dc-4b7b-9528-77dfb749461c
    /// - <c>URN:</c> urn:altinn:accesspackage:lonn
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til lønn og honorar. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Salary { get; } = new ConstantDefinition<Package>("fb0aa257-e7dc-4b7b-9528-77dfb749461c")
    {
        Entity = new()
        {
            Name = "Lønn",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til lønn og honorar. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:lonn",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Personnel,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Salary"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to salary and fees. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Lønn"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til lønn og honorar. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Lønn med personopplysninger av særlig kategori' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7c6d02b0-e0e9-45d6-b357-f2e929995475
    /// - <c>URN:</c> altinn:accesspackage:lonn-personopplysninger-saerlig-kategori
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til lønn og refusjon som inkluderer personopplysninger av særlig kategori. Denne fullmakten kan gi bruker tilgang til sensitive personopplysninger om ansatte, for eksempel knyttet til informasjon om ansattes sykefravær, foreldrepenger, pleiepenger eller lignende opplysninger. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> SalarySpecialCategory { get; } = new ConstantDefinition<Package>("7c6d02b0-e0e9-45d6-b357-f2e929995475")
    {
        Entity = new()
        {
            Name = "Lønn med personopplysninger av særlig kategori",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til lønn og refusjon som inkluderer personopplysninger av særlig kategori. Denne fullmakten kan gi bruker tilgang til sensitive personopplysninger om ansatte, for eksempel knyttet til informasjon om ansattes sykefravær, foreldrepenger, pleiepenger eller lignende opplysninger. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:lonn-personopplysninger-saerlig-kategori",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Personnel,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Salary with personal data of special category"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to salary and reimbursement that include personal data of special categories. This authorization may give the user access to sensitive personal information about employees, for example related to information about employees' sick leave, parental benefits, care benefits or similar information. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Lønn med personopplysningar av særleg kategori"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til lønn og refusjon som inkluderer personopplysningar av særleg kategori. Denne fullmakta kan gi brukar tilgang til sensitive personopplysningar om tilsette, til dømes knytt til informasjon om tilsettes sjukefråvær, foreldrepengar, pleiepengar eller liknande opplysningar. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Sykefravær' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 08ec673d-9126-4ed0-ae75-7ceec8633c77
    /// - <c>URN:</c> urn:altinn:accesspackage:sykefravaer
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til sykefravær. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> SickLeave { get; } = new ConstantDefinition<Package>("08ec673d-9126-4ed0-ae75-7ceec8633c77")
    {
        Entity = new()
        {
            Name = "Sykefravær",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sykefravær. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:sykefravaer",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.Personnel,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Sick leave"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to sick leave. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Sjukefråvær"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til sjukefråvær. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Skatt, avgift, regnskap og toll

    /// <summary>
    /// Represents the 'Regnskap og økonomirapportering' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> b4e69b54-895e-42c5-bf0d-861a571cd282
    /// - <c>URN:</c> urn:altinn:accesspackage:regnskap-okonomi-rapport
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til regnskap og øknomirapportering som ikke tilhører skatt og merverdiavgift. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> AccountingAndEconomicReporting { get; } = new ConstantDefinition<Package>("b4e69b54-895e-42c5-bf0d-861a571cd282")
    {
        Entity = new()
        {
            Name = "Regnskap og økonomirapportering",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til regnskap og øknomirapportering som ikke tilhører skatt og merverdiavgift. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:regnskap-okonomi-rapport",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TaxFeesAccountingAndCustoms,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Accounting and economic reporting"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to accounting and economic reporting that do not belong to tax and value-added tax. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Rekneskap og økonomirapportering"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til rekneskap og økonomirapportering som ikkje høyrer til skatt og meirverdiavgift. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Revisorattesterer' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 1886712b-e077-445a-ab3f-8c8bdebccc67
    /// - <c>URN:</c> urn:altinn:accesspackage:revisorattesterer
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til alle tjenester som krever revisorattestering. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> AuditorAttestation { get; } = new ConstantDefinition<Package>("1886712b-e077-445a-ab3f-8c8bdebccc67")
    {
        Entity = new()
        {
            Name = "Revisorattesterer",
            Description = "Denne fullmakten gir tilgang til alle tjenester som krever revisorattestering. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:revisorattesterer",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TaxFeesAccountingAndCustoms,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Auditor attestation"),
            KeyValuePair.Create("Description", "This authorization provides access to all services that require auditor attestation. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Revisorattesterer"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til alle tenester som krev revisorattestering. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Skatt næring' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 1dba50d6-f604-48e9-bd41-82321b13e85c
    /// - <c>URN:</c> urn:altinn:accesspackage:skatt-naering
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til skatt for næringer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> BusinessTax { get; } = new ConstantDefinition<Package>("1dba50d6-f604-48e9-bd41-82321b13e85c")
    {
        Entity = new()
        {
            Name = "Skatt næring",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til skatt for næringer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:skatt-naering",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TaxFeesAccountingAndCustoms,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Business tax"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to tax for businesses. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Skatt næring"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til skatt for næringar. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Krav, betalinger og utlegg' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> a02fc602-872a-4671-b338-8b86a64b534a
    /// - <c>URN:</c> urn:altinn:accesspackage:krav-og-utlegg
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til krav og utlegg. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> ClaimsPaymentsAndEnforcement { get; } = new ConstantDefinition<Package>("a02fc602-872a-4671-b338-8b86a64b534a")
    {
        Entity = new()
        {
            Name = "Krav, betalinger og utlegg",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til krav og utlegg. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:krav-og-utlegg",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TaxFeesAccountingAndCustoms,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Claims, payments and enforcement"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to claims and enforcement. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Krav, betalingar og utlegg"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til krav og utlegg. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Kreditt- og oppgjørsordninger' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> eb9006a6-dbd5-4155-9174-91e182a56715
    /// - <c>URN:</c> urn:altinn:accesspackage:kreditt-og-oppgjoer
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til kreditt- og oppgjørsordninger. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> CreditAndSettlementArrangements { get; } = new ConstantDefinition<Package>("eb9006a6-dbd5-4155-9174-91e182a56715")
    {
        Entity = new()
        {
            Name = "Kreditt- og oppgjørsordninger",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til kreditt- og oppgjørsordninger. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:kreditt-og-oppgjoer",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TaxFeesAccountingAndCustoms,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Credit and settlement arrangements"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to credit and settlement arrangements. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kreditt- og oppgjerordningar"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til kreditt- og oppgjerordningar. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Toll' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> deb7f8ae-8427-469d-b824-937c146c0489
    /// - <c>URN:</c> urn:altinn:accesspackage:toll
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til toll og fortolling. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> Customs { get; } = new ConstantDefinition<Package>("deb7f8ae-8427-469d-b824-937c146c0489")
    {
        Entity = new()
        {
            Name = "Toll",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til toll og fortolling. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:toll",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TaxFeesAccountingAndCustoms,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Customs"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to customs and customs clearance. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Toll"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til toll og fortolling. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Motorvognavgifter' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4f4d4567-2384-49fa-b34b-84b4b77139d8
    /// - <c>URN:</c> urn:altinn:accesspackage:motorvognavgift
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til motorvognavgifter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> MotorVehicleTaxes { get; } = new ConstantDefinition<Package>("4f4d4567-2384-49fa-b34b-84b4b77139d8")
    {
        Entity = new()
        {
            Name = "Motorvognavgifter",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til motorvognavgifter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:motorvognavgift",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TaxFeesAccountingAndCustoms,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Motor vehicle taxes"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to motor vehicle taxes. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Motorvognavgifter"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til motorvognavgifter. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Særavgifter' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 74c1dcf6-a760-4065-83f0-8edc6dec5dba
    /// - <c>URN:</c> urn:altinn:accesspackage:saeravgifter
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til særavgifter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> SpecialTaxes { get; } = new ConstantDefinition<Package>("74c1dcf6-a760-4065-83f0-8edc6dec5dba")
    {
        Entity = new()
        {
            Name = "Særavgifter",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til særavgifter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:saeravgifter",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TaxFeesAccountingAndCustoms,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Special taxes"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to special taxes. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Særavgifter"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til særavgifter. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Skattegrunnlag' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4c859601-9b2b-4662-af39-846f4117ad7a
    /// - <c>URN:</c> urn:altinn:accesspackage:skattegrunnlag
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til innhenting av skattegrunnlag. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> TaxBase { get; } = new ConstantDefinition<Package>("4c859601-9b2b-4662-af39-846f4117ad7a")
    {
        Entity = new()
        {
            Name = "Skattegrunnlag",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til innhenting av skattegrunnlag. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:skattegrunnlag",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TaxFeesAccountingAndCustoms,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tax base"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to the collection of tax basis information. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Skattegrunnlag"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til innhenting av skattegrunnlag. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Merverdiavgift' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 9a61b136-7810-4939-ab6d-84938e9a12c6
    /// - <c>URN:</c> urn:altinn:accesspackage:merverdiavgift
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne tilgangspakken gir fullmakter til tjenester knyttet til merverdiavgift. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> ValueAddedTax { get; } = new ConstantDefinition<Package>("9a61b136-7810-4939-ab6d-84938e9a12c6")
    {
        Entity = new()
        {
            Name = "Merverdiavgift",
            Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til merverdiavgift. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:merverdiavgift",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TaxFeesAccountingAndCustoms,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Value added tax"),
            KeyValuePair.Create("Description", "This access package provides authorizations for services related to value-added tax. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Meirverdiavgift"),
            KeyValuePair.Create("Description", "Denne tilgangspakken gir fullmakter til tenester knytt til meirverdiavgift. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

    #region Transport og lagring

    /// <summary>
    /// Represents the 'Lufttransport' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 3b3bc323-207b-4d56-a21f-97b6875ccc28
    /// - <c>URN:</c> urn:altinn:accesspackage:lufttransport
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester knyttet til luftfartøy og romfartøy. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> AirTransport { get; } = new ConstantDefinition<Package>("3b3bc323-207b-4d56-a21f-97b6875ccc28")
    {
        Entity = new()
        {
            Name = "Lufttransport",
            Description = "Denne fullmakten gir tilgang til tjenester knyttet til luftfartøy og romfartøy. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:lufttransport",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TransportAndStorage,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Air transport"),
            KeyValuePair.Create("Description", "This authorization provides access to services related to aircraft and spacecraft. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Lufttransport"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester knytt til luftfartøy og romfartøy. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Sjøfart' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 9603dd83-be41-4190-b0a7-97490f4a601d
    /// - <c>URN:</c> urn:altinn:accesspackage:sjofart
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester knyttet til skipsarbeidstakere og fartøy til sjøs. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> MaritimeTransport { get; } = new ConstantDefinition<Package>("9603dd83-be41-4190-b0a7-97490f4a601d")
    {
        Entity = new()
        {
            Name = "Sjøfart",
            Description = "Denne fullmakten gir tilgang til tjenester knyttet til skipsarbeidstakere og fartøy til sjøs. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:sjofart",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TransportAndStorage,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Maritime transport"),
            KeyValuePair.Create("Description", "This authorization provides access to services related to ship workers and vessels at sea. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Sjøfart"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester knytt til skipsarbeidstakarar og fartøy til sjøs. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Transport i rør' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 9cc8b401-b4cb-473d-a3fa-96171aeb1389
    /// - <c>URN:</c> urn:altinn:accesspackage:transport-i-ror
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester knyttet til transport i rør. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> PipelineTransport { get; } = new ConstantDefinition<Package>("9cc8b401-b4cb-473d-a3fa-96171aeb1389")
    {
        Entity = new()
        {
            Name = "Transport i rør",
            Description = "Denne fullmakten gir tilgang til tjenester knyttet til transport i rør. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:transport-i-ror",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TransportAndStorage,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Pipeline transport"),
            KeyValuePair.Create("Description", "This authorization provides access to services related to pipeline transport. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Transport i røyr"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester knytt til transport i røyr. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Jernbanetransport' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> dfcd3923-cb66-45f7-9535-999b6c0c496d
    /// - <c>URN:</c> urn:altinn:accesspackage:jernbanetransport
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester knyttet til jernbane, inkludert trikk, T-bane og sporvogn. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> RailwayTransport { get; } = new ConstantDefinition<Package>("dfcd3923-cb66-45f7-9535-999b6c0c496d")
    {
        Entity = new()
        {
            Name = "Jernbanetransport",
            Description = "Denne fullmakten gir tilgang til tjenester knyttet til jernbane, inkludert trikk, T-bane og sporvogn. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:jernbanetransport",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TransportAndStorage,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Railway transport"),
            KeyValuePair.Create("Description", "This authorization provides access to services related to railways, including trams, subways and trolleys. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Jernbanetransport"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester knytt til jernbane, inkludert trikk, T-bane og sporvogn. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Veitransport' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 36c245db-3207-449e-92b1-94cb0a0f3031
    /// - <c>URN:</c> urn:altinn:accesspackage:veitransport
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester knyttet til person- og godstransport langs veinettet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> RoadTransport { get; } = new ConstantDefinition<Package>("36c245db-3207-449e-92b1-94cb0a0f3031")
    {
        Entity = new()
        {
            Name = "Veitransport",
            Description = "Denne fullmakten gir tilgang til tjenester knyttet til person- og godstransport langs veinettet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:veitransport",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TransportAndStorage,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Road transport"),
            KeyValuePair.Create("Description", "This authorization provides access to services related to passenger and freight transport along the road network. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Veitransport"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester knytt til person- og godstransport langs vegnettet. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    /// <summary>
    /// Represents the 'Lagring og andre tjenester tilknyttet transport' access package.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 86a8ff87-3a3d-4f32-a4ef-99c43494bc6e
    /// - <c>URN:</c> urn:altinn:accesspackage:lagring-og-andre-tjenester-tilknyttet-transport
    /// - <c>Provider:</c> Altinn3
    /// - <c>Description:</c> Denne fullmakten gir tilgang til tjenester knyttet til lagring og hjelpetjenester i forbindelse med transport, samt post- og kurervirksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.
    /// </remarks>
    public static ConstantDefinition<Package> StorageTransportServices { get; } = new ConstantDefinition<Package>("86a8ff87-3a3d-4f32-a4ef-99c43494bc6e")
    {
        Entity = new()
        {
            Name = "Lagring og andre tjenester tilknyttet transport",
            Description = "Denne fullmakten gir tilgang til tjenester knyttet til lagring og hjelpetjenester i forbindelse med transport, samt post- og kurervirksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            Urn = "urn:altinn:accesspackage:lagring-og-andre-tjenester-tilknyttet-transport",
            IsDelegable = true,
            HasResources = true,
            IsAssignable = true,
            EntityTypeId = EntityTypeConstants.Organisation,
            ProviderId = ProviderConstants.Altinn3,
            AreaId = AreaConstants.TransportAndStorage,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Storage and other transport-related services"),
            KeyValuePair.Create("Description", "This authorization provides access to services related to storage and auxiliary services in connection with transport, as well as postal and courier activities. In the event of regulatory changes or the introduction of new digital services, there may be changes in the access that the authorization provides.")
        ),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Lagring og andre tenester tilknytt transport"),
            KeyValuePair.Create("Description", "Denne fullmakta gir tilgang til tenester knytt til lagring og hjelpetenester i samband med transport, samt post- og kurerverksemd. Ved regelverksendringar eller innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")
        ),
    };

    #endregion

}
