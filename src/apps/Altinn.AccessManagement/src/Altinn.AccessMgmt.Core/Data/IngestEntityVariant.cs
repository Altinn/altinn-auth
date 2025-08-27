using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Data;

public partial class StaticDataIngest
{
    /// <summary>
    /// Ingest EntityVariant
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task IngestEntityVariant(CancellationToken cancellationToken = default)
    {
        var orgTypeId = (await db.EntityTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Name == "Organisasjon", cancellationToken))?.Id ?? throw new KeyNotFoundException(string.Format("EntityType '{0}' not found", "Organisasjon"));
        var persTypeId = (await db.EntityTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Name == "Person", cancellationToken))?.Id ?? throw new KeyNotFoundException(string.Format("EntityType '{0}' not found", "Person"));
        var systemTypeId = (await db.EntityTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Name == "Systembruker", cancellationToken))?.Id ?? throw new KeyNotFoundException(string.Format("EntityType '{0}' not found", "Systembruker"));
        var internalTypeId = (await db.EntityTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Name == "Intern", cancellationToken))?.Id ?? throw new KeyNotFoundException(string.Format("EntityType '{0}' not found", "Intern"));

        var data = new List<EntityVariant>()
        {
            new EntityVariant() { Id = Guid.Parse("d786bc0e-8e9e-4116-bfc2-0344207c9127"), TypeId = orgTypeId, Name = "SAM", Description = "Tingsrettslig sameie" },
            new EntityVariant() { Id = Guid.Parse("d0a08401-5ae0-4da9-a79a-1113a7746b60"), TypeId = orgTypeId, Name = "VPFO", Description = "Verdipapirfond" },
            new EntityVariant() { Id = Guid.Parse("c161a605-3c72-40f2-8a5a-15e57e49638c"), TypeId = orgTypeId, Name = "UTLA", Description = "Utenlandsk enhet" },
            new EntityVariant() { Id = Guid.Parse("752f87dc-b04f-42cb-becd-173935ec6164"), TypeId = orgTypeId, Name = "BO", Description = "Andre bo" },
            new EntityVariant() { Id = Guid.Parse("263762ec-54fc-4eae-b7a1-17e92eea9a5c"), TypeId = orgTypeId, Name = "AS", Description = "Aksjeselskap" },
            new EntityVariant() { Id = Guid.Parse("6b2449e7-af5a-4c4e-b475-1b75998ba804"), TypeId = orgTypeId, Name = "PK", Description = "Pensjonskasse" },
            new EntityVariant() { Id = Guid.Parse("ed5d05b6-588c-40fa-8885-2bd36f75ac34"), TypeId = orgTypeId, Name = "PERS", Description = "Andre enkeltpersoner som registreres i tilknyttet register" },
            new EntityVariant() { Id = Guid.Parse("e0444411-a021-4774-854c-2ed876ffd64e"), TypeId = orgTypeId, Name = "EOFG", Description = "Europeisk økonomisk foretaksgruppe" },
            new EntityVariant() { Id = Guid.Parse("441a2876-f15f-4007-9e2e-3d25acbd98ff"), TypeId = orgTypeId, Name = "SE", Description = "Europeisk selskap" },
            new EntityVariant() { Id = Guid.Parse("3d5a890a-51aa-4e8a-b53d-3ec4111fe9e9"), TypeId = orgTypeId, Name = "TVAM", Description = "Tvangsregistrert for MVA" },
            new EntityVariant() { Id = Guid.Parse("e5d4a90e-948a-4c61-965a-43dbbd0efddb"), TypeId = orgTypeId, Name = "GFS", Description = "Gjensidig forsikringsselskap" },
            new EntityVariant() { Id = Guid.Parse("a581992e-c9dd-4250-8a9e-4e91d9b55424"), TypeId = orgTypeId, Name = "FYLK", Description = "Fylkeskommune" },
            new EntityVariant() { Id = Guid.Parse("ab5013e9-4210-4ab3-9fc2-554fd78a1b03"), TypeId = orgTypeId, Name = "IKJP", Description = "Andre ikke-juridiske personer" },
            new EntityVariant() { Id = Guid.Parse("a90417b4-5fa9-4a01-bfd0-57a1069a000c"), TypeId = orgTypeId, Name = "NUF", Description = "Norskregistrert utenlandsk foretak" },
            new EntityVariant() { Id = Guid.Parse("fca69e4d-453c-4404-b057-5e188c603f4b"), TypeId = orgTypeId, Name = "ANS", Description = "Ansvarlig selskap med solidarisk ansvar" },
            new EntityVariant() { Id = Guid.Parse("64b05309-7b6e-40c8-bd29-62d8fa0bd5ec"), TypeId = orgTypeId, Name = "KS", Description = "Kommandittselskap" },
            new EntityVariant() { Id = Guid.Parse("90b3eb3b-87cb-4ec3-bc44-65630cd02a67"), TypeId = orgTypeId, Name = "SÆR", Description = "Annet foretak iflg. særskilt lov" },
            new EntityVariant() { Id = Guid.Parse("d0648c3e-1567-48dc-a7cf-6837653dbc12"), TypeId = orgTypeId, Name = "IKS", Description = "Interkommunalt selskap" },
            new EntityVariant() { Id = Guid.Parse("9d80264a-b968-45f8-b740-6a283cbc06ad"), TypeId = orgTypeId, Name = "STI", Description = "Stiftelse" },
            new EntityVariant() { Id = Guid.Parse("8aa09ac2-dd61-492f-9613-6fc0558ab6fb"), TypeId = orgTypeId, Name = "BBL", Description = "Boligbyggelag" },
            new EntityVariant() { Id = Guid.Parse("e9b021a9-b257-42c0-8460-717a95c883f6"), TypeId = orgTypeId, Name = "KTRF", Description = "Kontorfellesskap" },
            new EntityVariant() { Id = Guid.Parse("2587990f-b036-4a6b-a7d9-815853be1382"), TypeId = orgTypeId, Name = "ANNA", Description = "Annen juridisk person" },
            new EntityVariant() { Id = Guid.Parse("7d356f18-2f72-49b5-a6f2-83d7c0871991"), TypeId = orgTypeId, Name = "SA", Description = "Samvirkeforetak" },
            new EntityVariant() { Id = Guid.Parse("e57cac52-e401-4c0f-a1cf-8bb4628fe671"), TypeId = orgTypeId, Name = "ADOS", Description = "Administrativ enhet - offentlig sektor" },
            new EntityVariant() { Id = Guid.Parse("3ae468d4-ea92-471d-b7b1-924e49b0d619"), TypeId = orgTypeId, Name = "KF", Description = "Kommunalt foretak" },
            new EntityVariant() { Id = Guid.Parse("0c31bb8f-587a-416b-a3cd-980bb73c5612"), TypeId = orgTypeId, Name = "AAFY", Description = "Underenhet til ikke-næringsdrivende" },
            new EntityVariant() { Id = Guid.Parse("b3433097-38b9-4a47-bd50-a4bb794cab3d"), TypeId = orgTypeId, Name = "DA", Description = "Ansvarlig selskap med delt ansvar" },
            new EntityVariant() { Id = Guid.Parse("4effb14f-8a1f-4272-aefb-b92ee302050f"), TypeId = orgTypeId, Name = "OPMV", Description = "Særskilt oppdelt enhet, jf. mval. § 2-2" },
            new EntityVariant() { Id = Guid.Parse("4f6c04d2-7223-41cc-8135-bb91d79ed311"), TypeId = orgTypeId, Name = "ORGL", Description = "Organisasjonsledd" },
            new EntityVariant() { Id = Guid.Parse("28157281-cc8f-46e0-9e2a-c20cb3b72930"), TypeId = orgTypeId, Name = "STAT", Description = "Staten" },
            new EntityVariant() { Id = Guid.Parse("d5b0abc8-22e7-44bb-bb55-c33bd6d7df4d"), TypeId = orgTypeId, Name = "SF", Description = "Statsforetak" },
            new EntityVariant() { Id = Guid.Parse("3aded080-d0d4-4893-8d30-c45dff4d7656"), TypeId = orgTypeId, Name = "PRE", Description = "Partrederi" },
            new EntityVariant() { Id = Guid.Parse("7c0ae1b2-2fa9-4266-8911-c4cb82c1489b"), TypeId = orgTypeId, Name = "BRL", Description = "Borettslag" },
            new EntityVariant() { Id = Guid.Parse("ed82281c-a5a1-4a28-9046-c70d95ce4658"), TypeId = orgTypeId, Name = "KOMM", Description = "Kommune" },
            new EntityVariant() { Id = Guid.Parse("ecd6e878-9121-43e6-aec0-c74b562cd3da"), TypeId = orgTypeId, Name = "FLI", Description = "Forening/lag/innretning" },
            new EntityVariant() { Id = Guid.Parse("80acaf52-3bf5-48c7-ab79-cb6f141a5b6f"), TypeId = orgTypeId, Name = "SPA", Description = "Sparebank" },
            new EntityVariant() { Id = Guid.Parse("ca45ed3a-41b3-4d2b-add9-db0d41a4e42b"), TypeId = orgTypeId, Name = "ASA", Description = "Allmennaksjeselskap" },
            new EntityVariant() { Id = Guid.Parse("1e2e44c0-e5e6-4962-8beb-e0ce16760a04"), TypeId = orgTypeId, Name = "ESEK", Description = "Eierseksjonssameie" },
            new EntityVariant() { Id = Guid.Parse("d78400f0-27d9-488a-886c-e264cc5c77ba"), TypeId = orgTypeId, Name = "ENK", Description = "Enkeltpersonforetak" },
            new EntityVariant() { Id = Guid.Parse("6b798668-a98d-49f2-a6d9-e391bad99fb2"), TypeId = orgTypeId, Name = "FKF", Description = "Fylkeskommunalt foretak" },
            new EntityVariant() { Id = Guid.Parse("7b43c6c2-e8ce-4f63-bb46-e4eb830fa222"), TypeId = orgTypeId, Name = "KIRK", Description = "Den norske kirke" },
            new EntityVariant() { Id = Guid.Parse("1f1e3720-b8a8-490e-8304-e81da21e3d3b"), TypeId = orgTypeId, Name = "BEDR", Description = "Underenhet til næringsdrivende og offentlig forvaltning" },
            new EntityVariant() { Id = Guid.Parse("d7208d54-067d-4b5c-a906-f0da3d3de0f1"), TypeId = orgTypeId, Name = "KBO", Description = "Konkursbo" },
            new EntityVariant() { Id = Guid.Parse("ea460099-515f-4e54-88d8-fbe53a807276"), TypeId = orgTypeId, Name = "BA", Description = "Selskap med begrenset ansvar" },
            new EntityVariant() { Id = Guid.Parse("b0690e14-7a75-45a4-8c02-437f6705b5ee"), TypeId = persTypeId, Name = "Person", Description = "Person" },
            new EntityVariant() { Id = Guid.Parse("8CA2FFDB-B4A9-4C64-8A9A-ED0F8DD722A3"), TypeId = systemTypeId, Name = "AgentSystem", Description = "AgentSystem" },
            new EntityVariant() { Id = Guid.Parse("f948baa3-8f6b-4790-a35c-85064c1b7f9b"), TypeId = systemTypeId, Name = "StandardSystem", Description = "StandardSystem" },
            new EntityVariant() { Id = Guid.Parse("03D08113-40D0-48BD-85B6-BD4430CCC182"), TypeId = persTypeId, Name = "SI", Description = "Selvidentifisert bruker" },
            new EntityVariant() { Id = Guid.Parse("CBE2834D-3DB0-4A14-BAA2-D32DE004D6D7"), TypeId = internalTypeId, Name = "Standard", Description = "Standard intern entitet" },
        };

        var translations = new List<TranslationEntryList>()
        {
            //// ENG
            new TranslationEntryList() { Id = Guid.Parse("d786bc0e-8e9e-4116-bfc2-0344207c9127"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "SAM" } , { "Description", "Legal co-ownership" } } },
            new TranslationEntryList() { Id = Guid.Parse("d0a08401-5ae0-4da9-a79a-1113a7746b60"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "VPFO" } , { "Description", "Securities fund" } } },
            new TranslationEntryList() { Id = Guid.Parse("c161a605-3c72-40f2-8a5a-15e57e49638c"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "UTLA" } , { "Description", "Foreign entity" } } },
            new TranslationEntryList() { Id = Guid.Parse("752f87dc-b04f-42cb-becd-173935ec6164"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "BO" } , { "Description", "Other estate" } } },
            new TranslationEntryList() { Id = Guid.Parse("263762ec-54fc-4eae-b7a1-17e92eea9a5c"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "AS" } , { "Description", "Limited company" } } },
            new TranslationEntryList() { Id = Guid.Parse("6b2449e7-af5a-4c4e-b475-1b75998ba804"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "PK" } , { "Description", "Pension fund" } } },
            new TranslationEntryList() { Id = Guid.Parse("ed5d05b6-588c-40fa-8885-2bd36f75ac34"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "PERS" } , { "Description", "Other individuals registered in the associated register" } } },
            new TranslationEntryList() { Id = Guid.Parse("e0444411-a021-4774-854c-2ed876ffd64e"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "EOFG" } , { "Description", "European Economic Interest Grouping" } } },
            new TranslationEntryList() { Id = Guid.Parse("441a2876-f15f-4007-9e2e-3d25acbd98ff"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "SE" } , { "Description", "European company" } } },
            new TranslationEntryList() { Id = Guid.Parse("3d5a890a-51aa-4e8a-b53d-3ec4111fe9e9"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "TVAM" } , { "Description", "Compulsory VAT registration" } } },
            new TranslationEntryList() { Id = Guid.Parse("e5d4a90e-948a-4c61-965a-43dbbd0efddb"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "GFS" } , { "Description", "Mutual insurance company" } } },
            new TranslationEntryList() { Id = Guid.Parse("a581992e-c9dd-4250-8a9e-4e91d9b55424"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "FYLK" } , { "Description", "County municipality" } } },
            new TranslationEntryList() { Id = Guid.Parse("ab5013e9-4210-4ab3-9fc2-554fd78a1b03"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "IKJP" } , { "Description", "Other non-legal persons" } } },
            new TranslationEntryList() { Id = Guid.Parse("a90417b4-5fa9-4a01-bfd0-57a1069a000c"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "NUF" } , { "Description", "Norwegian-registered foreign company" } } },
            new TranslationEntryList() { Id = Guid.Parse("fca69e4d-453c-4404-b057-5e188c603f4b"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "ANS" } , { "Description", "Partnership with joint liability" } } },
            new TranslationEntryList() { Id = Guid.Parse("64b05309-7b6e-40c8-bd29-62d8fa0bd5ec"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "KS" } , { "Description", "Limited partnership" } } },
            new TranslationEntryList() { Id = Guid.Parse("90b3eb3b-87cb-4ec3-bc44-65630cd02a67"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "SÆR" } , { "Description", "Other company according to special law" } } },
            new TranslationEntryList() { Id = Guid.Parse("d0648c3e-1567-48dc-a7cf-6837653dbc12"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "IKS" } , { "Description", "Inter-municipal company" } } },
            new TranslationEntryList() { Id = Guid.Parse("9d80264a-b968-45f8-b740-6a283cbc06ad"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "STI" } , { "Description", "Foundation" } } },
            new TranslationEntryList() { Id = Guid.Parse("8aa09ac2-dd61-492f-9613-6fc0558ab6fb"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "BBL" } , { "Description", "Housing cooperative" } } },
            new TranslationEntryList() { Id = Guid.Parse("e9b021a9-b257-42c0-8460-717a95c883f6"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "KTRF" } , { "Description", "Shared office" } } },
            new TranslationEntryList() { Id = Guid.Parse("2587990f-b036-4a6b-a7d9-815853be1382"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "ANNA" } , { "Description", "Other legal entity" } } },
            new TranslationEntryList() { Id = Guid.Parse("7d356f18-2f72-49b5-a6f2-83d7c0871991"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "SA" } , { "Description", "Cooperative enterprise" } } },
            new TranslationEntryList() { Id = Guid.Parse("e57cac52-e401-4c0f-a1cf-8bb4628fe671"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "ADOS" } , { "Description", "Administrative unit - public sector" } } },
            new TranslationEntryList() { Id = Guid.Parse("3ae468d4-ea92-471d-b7b1-924e49b0d619"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "KF" } , { "Description", "Municipal enterprise" } } },
            new TranslationEntryList() { Id = Guid.Parse("0c31bb8f-587a-416b-a3cd-980bb73c5612"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "AAFY" } , { "Description", "Subunit of non-commercial entity" } } },
            new TranslationEntryList() { Id = Guid.Parse("b3433097-38b9-4a47-bd50-a4bb794cab3d"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "DA" } , { "Description", "Partnership with divided liability" } } },
            new TranslationEntryList() { Id = Guid.Parse("4effb14f-8a1f-4272-aefb-b92ee302050f"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "OPMV" } , { "Description", "Specially divided unit, cf. VAT Act § 2-2" } } },
            new TranslationEntryList() { Id = Guid.Parse("4f6c04d2-7223-41cc-8135-bb91d79ed311"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "ORGL" } , { "Description", "Organizational unit" } } },
            new TranslationEntryList() { Id = Guid.Parse("28157281-cc8f-46e0-9e2a-c20cb3b72930"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "STAT" } , { "Description", "The State" } } },
            new TranslationEntryList() { Id = Guid.Parse("d5b0abc8-22e7-44bb-bb55-c33bd6d7df4d"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "SF" } , { "Description", "State enterprise" } } },
            new TranslationEntryList() { Id = Guid.Parse("3aded080-d0d4-4893-8d30-c45dff4d7656"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "PRE" } , { "Description", "Partnership for ship ownership" } } },
            new TranslationEntryList() { Id = Guid.Parse("7c0ae1b2-2fa9-4266-8911-c4cb82c1489b"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "BRL" } , { "Description", "Housing association" } } },
            new TranslationEntryList() { Id = Guid.Parse("ed82281c-a5a1-4a28-9046-c70d95ce4658"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "KOMM" } , { "Description", "Municipality" } } },
            new TranslationEntryList() { Id = Guid.Parse("ecd6e878-9121-43e6-aec0-c74b562cd3da"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "FLI" } , { "Description", "Association/club/institution" } } },
            new TranslationEntryList() { Id = Guid.Parse("80acaf52-3bf5-48c7-ab79-cb6f141a5b6f"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "SPA" } , { "Description", "Savings bank" } } },
            new TranslationEntryList() { Id = Guid.Parse("ca45ed3a-41b3-4d2b-add9-db0d41a4e42b"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "ASA" } , { "Description", "Public limited company" } } },
            new TranslationEntryList() { Id = Guid.Parse("1e2e44c0-e5e6-4962-8beb-e0ce16760a04"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "ESEK" } , { "Description", "Condominium" } } },
            new TranslationEntryList() { Id = Guid.Parse("d78400f0-27d9-488a-886c-e264cc5c77ba"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "ENK" } , { "Description", "Sole proprietorship" } } },
            new TranslationEntryList() { Id = Guid.Parse("6b798668-a98d-49f2-a6d9-e391bad99fb2"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "FKF" } , { "Description", "County municipal enterprise" } } },
            new TranslationEntryList() { Id = Guid.Parse("7b43c6c2-e8ce-4f63-bb46-e4eb830fa222"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "KIRK" } , { "Description", "The Church of Norway" } } },
            new TranslationEntryList() { Id = Guid.Parse("1f1e3720-b8a8-490e-8304-e81da21e3d3b"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "BEDR" } , { "Description", "Subunit of commercial and public administration" } } },
            new TranslationEntryList() { Id = Guid.Parse("d7208d54-067d-4b5c-a906-f0da3d3de0f1"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "KBO" } , { "Description", "Bankruptcy estate" } } },
            new TranslationEntryList() { Id = Guid.Parse("ea460099-515f-4e54-88d8-fbe53a807276"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "BA" } , { "Description", "Limited liability company" } } },
            new TranslationEntryList() { Id = Guid.Parse("b0690e14-7a75-45a4-8c02-437f6705b5ee"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "Person" } , { "Description", "Person" } } },
            new TranslationEntryList() { Id = Guid.Parse("8CA2FFDB-B4A9-4C64-8A9A-ED0F8DD722A3"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "AgentSystem" } , { "Description", "AgentSystem" } } },
            new TranslationEntryList() { Id = Guid.Parse("f948baa3-8f6b-4790-a35c-85064c1b7f9b"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "StandardSystem" } , { "Description", "StandardSystem" } } },
            new TranslationEntryList() { Id = Guid.Parse("03D08113-40D0-48BD-85B6-BD4430CCC182"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "SI" } , { "Description", "Self-identified user" } } },
            new TranslationEntryList() { Id = Guid.Parse("CBE2834D-3DB0-4A14-BAA2-D32DE004D6D7"), Type = nameof(EntityVariant),LanguageCode = "eng", Translations = { { "Name", "Default" } , { "Description", "Default internal entity" } } },
            //// NNO
            new TranslationEntryList() { Id = Guid.Parse("d786bc0e-8e9e-4116-bfc2-0344207c9127"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "SAM" } , { "Description" , "Tingsrettslig sameie" } } },
            new TranslationEntryList() { Id = Guid.Parse("d0a08401-5ae0-4da9-a79a-1113a7746b60"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "VPFO" } , { "Description" , "Verdipapirfond" } } },
            new TranslationEntryList() { Id = Guid.Parse("c161a605-3c72-40f2-8a5a-15e57e49638c"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "UTLA" } , { "Description" , "Utanlandsk eining" } } },
            new TranslationEntryList() { Id = Guid.Parse("752f87dc-b04f-42cb-becd-173935ec6164"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "BO" } , { "Description" , "Andre bo" } } },
            new TranslationEntryList() { Id = Guid.Parse("263762ec-54fc-4eae-b7a1-17e92eea9a5c"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "AS" } , { "Description" , "Aksjeselskap" } } },
            new TranslationEntryList() { Id = Guid.Parse("6b2449e7-af5a-4c4e-b475-1b75998ba804"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "PK" } , { "Description" , "Pensjonskasse" } } },
            new TranslationEntryList() { Id = Guid.Parse("ed5d05b6-588c-40fa-8885-2bd36f75ac34"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "PERS" } , { "Description" , "Andre enkeltpersonar som registrerast i tilknytta register" } } },
            new TranslationEntryList() { Id = Guid.Parse("e0444411-a021-4774-854c-2ed876ffd64e"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "EOFG" } , { "Description" , "Europeisk økonomisk foretaksgruppe" } } },
            new TranslationEntryList() { Id = Guid.Parse("441a2876-f15f-4007-9e2e-3d25acbd98ff"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "SE" } , { "Description" , "Europeisk selskap" } } },
            new TranslationEntryList() { Id = Guid.Parse("3d5a890a-51aa-4e8a-b53d-3ec4111fe9e9"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "TVAM" } , { "Description" , "Tvangsregistrert for MVA" } } },
            new TranslationEntryList() { Id = Guid.Parse("e5d4a90e-948a-4c61-965a-43dbbd0efddb"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "GFS" } , { "Description" , "Gjensidig forsikringsselskap" } } },
            new TranslationEntryList() { Id = Guid.Parse("a581992e-c9dd-4250-8a9e-4e91d9b55424"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "FYLK" } , { "Description" , "Fylkeskommune" } } },
            new TranslationEntryList() { Id = Guid.Parse("ab5013e9-4210-4ab3-9fc2-554fd78a1b03"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "IKJP" } , { "Description" , "Andre ikkje-juridiske personar" } } },
            new TranslationEntryList() { Id = Guid.Parse("a90417b4-5fa9-4a01-bfd0-57a1069a000c"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "NUF" } , { "Description" , "Norskregistrert utanlandsk foretak" } } },
            new TranslationEntryList() { Id = Guid.Parse("fca69e4d-453c-4404-b057-5e188c603f4b"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "ANS" } , { "Description" , "Ansvarleg selskap med solidarisk ansvar" } } },
            new TranslationEntryList() { Id = Guid.Parse("64b05309-7b6e-40c8-bd29-62d8fa0bd5ec"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "KS" } , { "Description" , "Kommandittselskap" } } },
            new TranslationEntryList() { Id = Guid.Parse("90b3eb3b-87cb-4ec3-bc44-65630cd02a67"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "SÆR" } , { "Description" , "Annet foretak i følgje særskild lov" } } },
            new TranslationEntryList() { Id = Guid.Parse("d0648c3e-1567-48dc-a7cf-6837653dbc12"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "IKS" } , { "Description" , "Interkommunalt selskap" } } },
            new TranslationEntryList() { Id = Guid.Parse("9d80264a-b968-45f8-b740-6a283cbc06ad"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "STI" } , { "Description" , "Stiftelse" } } },
            new TranslationEntryList() { Id = Guid.Parse("8aa09ac2-dd61-492f-9613-6fc0558ab6fb"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "BBL" } , { "Description" , "Boligbyggelag" } } },
            new TranslationEntryList() { Id = Guid.Parse("e9b021a9-b257-42c0-8460-717a95c883f6"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "KTRF" } , { "Description" , "Kontorfellesskap" } } },
            new TranslationEntryList() { Id = Guid.Parse("2587990f-b036-4a6b-a7d9-815853be1382"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "ANNA" } , { "Description" , "Annan juridisk person" } } },
            new TranslationEntryList() { Id = Guid.Parse("7d356f18-2f72-49b5-a6f2-83d7c0871991"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "SA" } , { "Description" , "Samvirkeforetak" } } },
            new TranslationEntryList() { Id = Guid.Parse("e57cac52-e401-4c0f-a1cf-8bb4628fe671"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "ADOS" } , { "Description" , "Administrativ eining - offentleg sektor" } } },
            new TranslationEntryList() { Id = Guid.Parse("3ae468d4-ea92-471d-b7b1-924e49b0d619"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "KF" } , { "Description" , "Kommunalt foretak" } } },
            new TranslationEntryList() { Id = Guid.Parse("0c31bb8f-587a-416b-a3cd-980bb73c5612"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "AAFY" } , { "Description" , "Underenhet til ikkje-næringsdrivande" } } },
            new TranslationEntryList() { Id = Guid.Parse("b3433097-38b9-4a47-bd50-a4bb794cab3d"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "DA" } , { "Description" , "Ansvarleg selskap med delt ansvar" } } },
            new TranslationEntryList() { Id = Guid.Parse("4effb14f-8a1f-4272-aefb-b92ee302050f"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "OPMV" } , { "Description" , "Særskild oppdelt eining, jf. mval. § 2-2" } } },
            new TranslationEntryList() { Id = Guid.Parse("4f6c04d2-7223-41cc-8135-bb91d79ed311"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "ORGL" } , { "Description" , "Organisasjonsledd" } } },
            new TranslationEntryList() { Id = Guid.Parse("28157281-cc8f-46e0-9e2a-c20cb3b72930"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "STAT" } , { "Description" , "Staten" } } },
            new TranslationEntryList() { Id = Guid.Parse("d5b0abc8-22e7-44bb-bb55-c33bd6d7df4d"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "SF" } , { "Description" , "Statsforetak" } } },
            new TranslationEntryList() { Id = Guid.Parse("3aded080-d0d4-4893-8d30-c45dff4d7656"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "PRE" } , { "Description" , "Partrederi" } } },
            new TranslationEntryList() { Id = Guid.Parse("7c0ae1b2-2fa9-4266-8911-c4cb82c1489b"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "BRL" } , { "Description" , "Borettslag" } } },
            new TranslationEntryList() { Id = Guid.Parse("ed82281c-a5a1-4a28-9046-c70d95ce4658"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "KOMM" } , { "Description" , "Kommune" } } },
            new TranslationEntryList() { Id = Guid.Parse("ecd6e878-9121-43e6-aec0-c74b562cd3da"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "FLI" } , { "Description" , "Forening/lag/innretting" } } },
            new TranslationEntryList() { Id = Guid.Parse("80acaf52-3bf5-48c7-ab79-cb6f141a5b6f"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "SPA" } , { "Description" , "Sparebank" } } },
            new TranslationEntryList() { Id = Guid.Parse("ca45ed3a-41b3-4d2b-add9-db0d41a4e42b"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "ASA" } , { "Description" , "Allmennaksjeselskap" } } },
            new TranslationEntryList() { Id = Guid.Parse("1e2e44c0-e5e6-4962-8beb-e0ce16760a04"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "ESEK" } , { "Description" , "Eigarseksjonssameie" } } },
            new TranslationEntryList() { Id = Guid.Parse("d78400f0-27d9-488a-886c-e264cc5c77ba"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "ENK" } , { "Description" , "Enkeltpersonforetak" } } },
            new TranslationEntryList() { Id = Guid.Parse("6b798668-a98d-49f2-a6d9-e391bad99fb2"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "FKF" } , { "Description" , "Fylkeskommunalt foretak" } } },
            new TranslationEntryList() { Id = Guid.Parse("7b43c6c2-e8ce-4f63-bb46-e4eb830fa222"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "KIRK" } , { "Description" , "Den norske kyrkja" } } },
            new TranslationEntryList() { Id = Guid.Parse("1f1e3720-b8a8-490e-8304-e81da21e3d3b"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "BEDR" } , { "Description" , "Underenhet til næringsdrivande og offentleg forvaltning" } } },
            new TranslationEntryList() { Id = Guid.Parse("d7208d54-067d-4b5c-a906-f0da3d3de0f1"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "KBO" } , { "Description" , "Konkursbo" } } },
            new TranslationEntryList() { Id = Guid.Parse("ea460099-515f-4e54-88d8-fbe53a807276"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "BA" } , { "Description" , "Selskap med avgrensa ansvar" } } },
            new TranslationEntryList() { Id = Guid.Parse("b0690e14-7a75-45a4-8c02-437f6705b5ee"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "PERS" } , { "Description" , "Person" } } },
            new TranslationEntryList() { Id = Guid.Parse("8CA2FFDB-B4A9-4C64-8A9A-ED0F8DD722A3"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "AgentSystem" } , { "Description" , "AgentSystem" } } },
            new TranslationEntryList() { Id = Guid.Parse("f948baa3-8f6b-4790-a35c-85064c1b7f9b"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "StandardSystem" } , { "Description" , "StandardSystem" } } },
            new TranslationEntryList() { Id = Guid.Parse("03D08113-40D0-48BD-85B6-BD4430CCC182"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "SI" } , { "Description" , "Sjølvidentifisert brukar" } } },
            new TranslationEntryList() { Id = Guid.Parse("CBE2834D-3DB0-4A14-BAA2-D32DE004D6D7"), Type = nameof(EntityVariant), LanguageCode = "nno", Translations = { { "Name", "Standard" } , { "Description" , "Standard intern entitet" } } },
        };

        db.Database.SetAuditSession(auditValues);

        foreach (var d in data)
        {
            var obj = db.EntityVariants.FirstOrDefault(t => t.Id == d.Id);
            if (obj == null)
            {
                db.EntityVariants.Add(d);
            }
            else
            {
                obj.Name = d.Name;
            }
        }

        foreach (var translation in translations.SelectMany(t => t.SingleEntries()))
        {
            await translationService.UpsertTranslationAsync(translation);
        }

        var result = await db.SaveChangesAsync();
    }
}
