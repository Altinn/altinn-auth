using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Data
{
    public partial class StaticDataIngest
    {
        private readonly string iconBaseUrl = configuration["AltinnCDN:AccessPackageIconsBaseURL"];

        public async Task IngestArea(CancellationToken cancellationToken = default)
        {
            var areaGroupAllmentId = (await db.AreaGroups.AsNoTracking().SingleOrDefaultAsync(t => t.Id == Guid.Parse("7E2A3AF8-08CB-43A9-BDD7-7D5C7E377145")))?.Id ?? throw new KeyNotFoundException("AreaGroup not found 'Allment'");
            var areaGroupBransjeId = (await db.AreaGroups.AsNoTracking().SingleOrDefaultAsync(t => t.Id == Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4")))?.Id ?? throw new KeyNotFoundException("AreaGroup not found 'Bransje'");
            var areaGroupSaerskiltId = (await db.AreaGroups.AsNoTracking().SingleOrDefaultAsync(t => t.Id == Guid.Parse("554F0321-53B8-4D97-BE12-6A585C507159")))?.Id ?? throw new KeyNotFoundException("AreaGroup not found 'Særskilt'");

            var data = new List<Area>()
            {
                // Allment
                new Area() { Id = Guid.Parse("7d32591d-34b7-4afc-8afa-013722f8c05d"), Urn = "accesspackage:area:skatt_avgift_regnskap_og_toll", Name = "Skatt, avgift, regnskap og toll", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til skatt, avgift, regnskap og toll.", IconUrl = $"{iconBaseUrl}Aksel_Money_SackKroner.svg", GroupId = areaGroupAllmentId },
                new Area() { Id = Guid.Parse("6f7f3b02-8b5a-4823-9468-0f4646d3a790"), Urn = "accesspackage:area:personale", Name = "Personale", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til personale.", IconUrl = $"{iconBaseUrl}Aksel_People_PersonGroup.svg", GroupId = areaGroupAllmentId },
                new Area() { Id = Guid.Parse("a8834a7c-ed89-4c73-b5d5-19a2347f3b13"), Urn = "accesspackage:area:miljo_ulykke_og_sikkerhet", Name = "Miljø, ulykke og sikkerhet", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til miljø, ulykke og sikkerhet.", IconUrl = $"{iconBaseUrl}Aksel_People_HandHeart.svg", GroupId = areaGroupAllmentId },
                new Area() { Id = Guid.Parse("6f938de8-34f2-4bab-a0c6-3a3eb64aad3b"), Urn = "accesspackage:area:post_og_arkiv", Name = "Post og arkiv", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til post og arkiv.", IconUrl = $"{iconBaseUrl}Aksel_Interface_EnvelopeClosed.svg", GroupId = areaGroupAllmentId },
                new Area() { Id = Guid.Parse("3f5df819-7aca-49e1-bf6f-3e8f120f20d1"), Urn = "accesspackage:area:forhold_ved_virksomheten", Name = "Forhold ved virksomheten", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til forhold ved virksomheten.", IconUrl = $"{iconBaseUrl}Aksel_Workplace_Buildings3.svg", GroupId = areaGroupAllmentId },
                new Area() { Id = Guid.Parse("892e98c6-1696-46e7-9bb1-59c08761ec64"), Urn = "accesspackage:area:integrasjoner", Name = "Integrasjoner", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til integrasjoner.", IconUrl = $"{iconBaseUrl}Aksel_Interface_RotateLeft.svg", GroupId = areaGroupAllmentId },
                new Area() { Id = Guid.Parse("e4ae823f-41db-46ed-873f-8a5d1378fff8"), Urn = "accesspackage:area:administrere_tilganger", Name = "Administrere tilganger", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til administrere tilganger.", IconUrl = $"{iconBaseUrl}Altinn_Administrere-tilganger_PersonLock.svg", GroupId = areaGroupAllmentId },

                // Bransje
                new Area() { Id = Guid.Parse("fc93d25e-80bc-469a-aa43-a6cee80eb3e2"), Urn = "accesspackage:area:jordbruk_skogbruk_jakt_fiske_og_akvakultur", Name = "Jordbruk, skogbruk, jakt, fiske og akvakultur", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til jordbruk, skogbruk, jakt, fiske og akvakultur.", IconUrl = $"{iconBaseUrl}Aksel_Nature-and-animals-Plant.svg", GroupId = areaGroupBransjeId },
                new Area() { Id = Guid.Parse("536b317c-ef85-45d4-9b48-6511578e1952"), Urn = "accesspackage:area:bygg_anlegg_og_eiendom", Name = "Bygg, anlegg og eiendom", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til bygg, anlegg og eiendom.", IconUrl = $"{iconBaseUrl}Altinn_Bygg-anlegg-og-eiendom_HandHouse.svg", GroupId = areaGroupBransjeId },
                new Area() { Id = Guid.Parse("6ff90072-566b-4acd-baac-ec477534e712"), Urn = "accesspackage:area:transport_og_lagring", Name = "Transport og lagring", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til transport og lagring.", IconUrl = $"{iconBaseUrl}Aksel_Transportation_Truck.svg", GroupId = areaGroupBransjeId },
                new Area() { Id = Guid.Parse("eab59b26-833f-40ca-9e27-72107e8f1908"), Urn = "accesspackage:area:helse_pleie_omsorg_og_vern", Name = "Helse, pleie, omsorg og vern", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til helse, pleie, omsorg og vern.", IconUrl = $"{iconBaseUrl}Aksel_Wellness_Hospital.svg", GroupId = areaGroupBransjeId },
                new Area() { Id = Guid.Parse("7326614f-cf7c-492e-8e7f-d74e6e4a8970"), Urn = "accesspackage:area:oppvekst_og_utdanning", Name = "Oppvekst og utdanning", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til oppvekst og utdanning.", IconUrl = $"{iconBaseUrl}Aksel_Workplace_Buildings2.svg", GroupId = areaGroupBransjeId },
                new Area() { Id = Guid.Parse("6e152c10-0f63-4060-9b14-66808e7ac320"), Urn = "accesspackage:area:energi_vann_avlop_og_avfall", Name = "Energi, vann, avløp og avfall", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til energi, vann, avløp og avfall.", IconUrl = $"{iconBaseUrl}Aksel_Workplace_TapWater.svg", GroupId = areaGroupBransjeId },
                new Area() { Id = Guid.Parse("10c2dd29-5ab3-4a26-900e-8e2326150353"), Urn = "accesspackage:area:industrier", Name = "Industrier", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til industrier.", IconUrl = $"{iconBaseUrl}Altinn_Industrier_Factory.svg", GroupId = areaGroupBransjeId },
                new Area() { Id = Guid.Parse("5996ba37-6db0-4391-8918-b1b0bd4b394b"), Urn = "accesspackage:area:kultur_og_frivillighet", Name = "Kultur og frivillighet", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til kultur og frivillighet.", IconUrl = $"{iconBaseUrl}Aksel_Wellness_HeadHeart.svg", GroupId = areaGroupBransjeId },
                new Area() { Id = Guid.Parse("3797e9f0-dd83-404c-9897-e356c32ef600"), Urn = "accesspackage:area:handel_overnatting_og_servering", Name = "Handel, overnatting og servering", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til handel, overnatting og servering.", IconUrl = $"{iconBaseUrl}Aksel_Wellness_TrayFood.svg", GroupId = areaGroupBransjeId },
                new Area() { Id = Guid.Parse("e31169f6-d4c7-4e45-93c7-f90bc285b639"), Urn = "accesspackage:area:andre_tjenesteytende_naeringer", Name = "Andre tjenesteytende næringer", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til andre tjenesteytende næringer.", IconUrl = $"{iconBaseUrl}Aksel_Workplace_Reception.svg", GroupId = areaGroupBransjeId },

                // Særskilt
                new Area() { Id = Guid.Parse("64cbcdc8-01c9-448c-b3d2-eb9582beb3c2"), Urn = "accesspackage:area:fullmakter_for_regnskapsforer", Name = "Fullmakter for regnskapsfører", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for regnskapsfører.", IconUrl = $"{iconBaseUrl}Aksel_Home_Calculator.svg", GroupId = areaGroupSaerskiltId },
                new Area() { Id = Guid.Parse("7df15290-f43c-4831-a1b4-3edfa43e526d"), Urn = "accesspackage:area:fullmakter_for_revisor", Name = "Fullmakter for revisor", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for revisor.", IconUrl = $"{iconBaseUrl}Aksel_Files-and-application_FileSearch.svg", GroupId = areaGroupSaerskiltId },
                new Area() { Id = Guid.Parse("f3daddb7-6e21-455e-b6d2-65a281375b6b"), Urn = "accesspackage:area:fullmakter_for_konkursbo", Name = "Fullmakter for konkursbo", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for konkursbo.", IconUrl = $"{iconBaseUrl}Aksel_Statistics-and-math_TrendDown.svg", GroupId = areaGroupSaerskiltId },
                new Area() { Id = Guid.Parse("0195efb8-7c80-76b3-bb86-ae9dfd74bca2"), Urn = "accesspackage:area:fullmakter_for_forretningsforer", Name = "Fullmakter for forretningsfører", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for forretningsfører.", IconUrl = $"{iconBaseUrl}Aksel_Statistics-and-math_TrendDown.svg", GroupId = areaGroupSaerskiltId },
            };

            var translations = new List<TranslationEntryList>
            {
                // Skatt, avgift, regnskap og toll
                new TranslationEntryList { Id = Guid.Parse("7d32591d-34b7-4afc-8afa-013722f8c05d"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Taxes, Fees, Accounting and Customs" }, { "Description", "This authorization area includes access packages related to taxes, fees, accounting, and customs." } } },
                new TranslationEntryList { Id = Guid.Parse("7d32591d-34b7-4afc-8afa-013722f8c05d"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Skatt, avgift, rekneskap og toll" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til skatt, avgift, rekneskap og toll." } } },

                // Personale
                new TranslationEntryList { Id = Guid.Parse("6f7f3b02-8b5a-4823-9468-0f4646d3a790"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Personnel" }, { "Description", "This authorization area includes access packages related to personnel." } } },
                new TranslationEntryList { Id = Guid.Parse("6f7f3b02-8b5a-4823-9468-0f4646d3a790"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Personale" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til personalet." } } },

                // Miljø, ulykke og sikkerhet
                new TranslationEntryList { Id = Guid.Parse("a8834a7c-ed89-4c73-b5d5-19a2347f3b13"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Environment, Accident and Safety" }, { "Description", "This authorization area includes access packages related to environment, accident, and safety." } } },
                new TranslationEntryList { Id = Guid.Parse("a8834a7c-ed89-4c73-b5d5-19a2347f3b13"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Miljø, ulykke og tryggleik" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til miljø, ulykke og tryggleik." } } },

                // Post og arkiv
                new TranslationEntryList { Id = Guid.Parse("6f938de8-34f2-4bab-a0c6-3a3eb64aad3b"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Mail and Archive" }, { "Description", "This authorization area includes access packages related to mail and archive." } } },
                new TranslationEntryList { Id = Guid.Parse("6f938de8-34f2-4bab-a0c6-3a3eb64aad3b"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Post og arkiv" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til post og arkiv." } } },

                // Forhold ved virksomheten
                new TranslationEntryList { Id = Guid.Parse("3f5df819-7aca-49e1-bf6f-3e8f120f20d1"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Business Affairs" }, { "Description", "This authorization area includes access packages related to business affairs." } } },
                new TranslationEntryList { Id = Guid.Parse("3f5df819-7aca-49e1-bf6f-3e8f120f20d1"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Forhold ved verksemda" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til forhold ved verksemda." } } },

                // Integrasjoner
                new TranslationEntryList { Id = Guid.Parse("892e98c6-1696-46e7-9bb1-59c08761ec64"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Integrations" }, { "Description", "This authorization area includes access packages related to integrations." } } },
                new TranslationEntryList { Id = Guid.Parse("892e98c6-1696-46e7-9bb1-59c08761ec64"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Integrasjonar" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til integrasjonar." } } },

                // Administrere tilganger
                new TranslationEntryList { Id = Guid.Parse("e4ae823f-41db-46ed-873f-8a5d1378fff8"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Manage Access" }, { "Description", "This authorization area includes access packages related to managing access." } } },
                new TranslationEntryList { Id = Guid.Parse("e4ae823f-41db-46ed-873f-8a5d1378fff8"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Administrere tilgongar" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til administrering av tilgongar." } } },

                // Jordbruk, skogbruk, jakt, fiske og akvakultur
                new TranslationEntryList { Id = Guid.Parse("fc93d25e-80bc-469a-aa43-a6cee80eb3e2"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Agriculture, Forestry, Hunting, Fishing and Aquaculture" }, { "Description", "This authorization area includes access packages related to agriculture, forestry, hunting, fishing and aquaculture." } } },
                new TranslationEntryList { Id = Guid.Parse("fc93d25e-80bc-469a-aa43-a6cee80eb3e2"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Jordbruk, skogbruk, jakt, fiske og akvakultur" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til jordbruk, skogbruk, jakt, fiske og akvakultur." } } },

                // Bygg, anlegg og eiendom
                new TranslationEntryList { Id = Guid.Parse("536b317c-ef85-45d4-9b48-6511578e1952"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Construction, Infrastructure and Real Estate" }, { "Description", "This authorization area includes access packages related to construction, infrastructure and real estate." } } },
                new TranslationEntryList { Id = Guid.Parse("536b317c-ef85-45d4-9b48-6511578e1952"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Bygg, anlegg og eigedom" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til bygg, anlegg og eigedom." } } },

                // Transport og lagring
                new TranslationEntryList { Id = Guid.Parse("6ff90072-566b-4acd-baac-ec477534e712"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Transport and Storage" }, { "Description", "This authorization area includes access packages related to transport and storage." } } },
                new TranslationEntryList { Id = Guid.Parse("6ff90072-566b-4acd-baac-ec477534e712"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Transport og lagring" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til transport og lagring." } } },

                // Helse, pleie, omsorg og vern
                new TranslationEntryList { Id = Guid.Parse("eab59b26-833f-40ca-9e27-72107e8f1908"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Health, Care and Protection" }, { "Description", "This authorization area includes access packages related to health, care and protection." } } },
                new TranslationEntryList { Id = Guid.Parse("eab59b26-833f-40ca-9e27-72107e8f1908"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Helse, pleie, omsorg og vern" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til helse, pleie, omsorg og vern." } } },

                // Oppvekst og utdanning
                new TranslationEntryList { Id = Guid.Parse("7326614f-cf7c-492e-8e7f-d74e6e4a8970"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Childhood and Education" }, { "Description", "This authorization area includes access packages related to childhood and education." } } },
                new TranslationEntryList { Id = Guid.Parse("7326614f-cf7c-492e-8e7f-d74e6e4a8970"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Oppvekst og utdanning" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til oppvekst og utdanning." } } },

                // Energi, vann, avløp og avfall
                new TranslationEntryList { Id = Guid.Parse("6e152c10-0f63-4060-9b14-66808e7ac320"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Energy, Water, Sewage and Waste" }, { "Description", "This authorization area includes access packages related to energy, water, sewage and waste." } } },
                new TranslationEntryList { Id = Guid.Parse("6e152c10-0f63-4060-9b14-66808e7ac320"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Energi, vann, avløp og avfall" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til energi, vann, avløp og avfall." } } },

                // Industrier
                new TranslationEntryList { Id = Guid.Parse("10c2dd29-5ab3-4a26-900e-8e2326150353"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Industries" }, { "Description", "This authorization area includes access packages related to industries." } } },
                new TranslationEntryList { Id = Guid.Parse("10c2dd29-5ab3-4a26-900e-8e2326150353"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Industriar" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til industriar." } } },

                // Kultur og frivillighet
                new TranslationEntryList { Id = Guid.Parse("5996ba37-6db0-4391-8918-b1b0bd4b394b"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Culture and Volunteering" }, { "Description", "This authorization area includes access packages related to culture and volunteering." } } },
                new TranslationEntryList { Id = Guid.Parse("5996ba37-6db0-4391-8918-b1b0bd4b394b"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Kultur og frivillighet" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til kultur og frivillighet." } } },

                // Handel, overnatting og servering
                new TranslationEntryList { Id = Guid.Parse("3797e9f0-dd83-404c-9897-e356c32ef600"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Commerce, Accommodation and Catering" }, { "Description", "This authorization area includes access packages related to commerce, accommodation and catering." } } },
                new TranslationEntryList { Id = Guid.Parse("3797e9f0-dd83-404c-9897-e356c32ef600"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Handel, overnatting og servering" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til handel, overnatting og servering." } } },

                // Andre tjenesteytende næringer
                new TranslationEntryList { Id = Guid.Parse("e31169f6-d4c7-4e45-93c7-f90bc285b639"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Other Service Industries" }, { "Description", "This authorization area includes access packages related to other service industries." } } },
                new TranslationEntryList { Id = Guid.Parse("e31169f6-d4c7-4e45-93c7-f90bc285b639"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Andre tenesteytande næringar" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til andre tenesteytande næringar." } } },

                // Fullmakter for regnskapsfører
                new TranslationEntryList { Id = Guid.Parse("64cbcdc8-01c9-448c-b3d2-eb9582beb3c2"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Authorizations for Accountants" }, { "Description", "This authorization area includes access packages related to authorizations for accountants." } } },
                new TranslationEntryList { Id = Guid.Parse("64cbcdc8-01c9-448c-b3d2-eb9582beb3c2"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Fullmakter for rekneskapsførar" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til fullmakter for rekneskapsførar." } } },

                // Fullmakter for revisor
                new TranslationEntryList { Id = Guid.Parse("7df15290-f43c-4831-a1b4-3edfa43e526d"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Authorizations for Auditors" }, { "Description", "This authorization area includes access packages related to authorizations for auditors." } } },
                new TranslationEntryList { Id = Guid.Parse("7df15290-f43c-4831-a1b4-3edfa43e526d"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Fullmakter for revisor" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til fullmakter for revisor." } } },

                // Fullmakter for konkursbo
                new TranslationEntryList { Id = Guid.Parse("f3daddb7-6e21-455e-b6d2-65a281375b6b"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Authorizations for Bankruptcy Estates" }, { "Description", "This authorization area includes access packages related to authorizations for bankruptcy estates." } } },
                new TranslationEntryList { Id = Guid.Parse("f3daddb7-6e21-455e-b6d2-65a281375b6b"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Fullmakter for konkursbo" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til fullmakter for konkursbo." } } },

                // Fullmakter for forretningsfører
                new TranslationEntryList { Id = Guid.Parse("0195efb8-7c80-76b3-bb86-ae9dfd74bca2"), LanguageCode = "eng", Type = nameof(Area), Translations = { { "Name", "Authorizations for Bussineses" }, { "Description", "This authorization area includes access packages related to authorizations for bussineses." } } },
                new TranslationEntryList { Id = Guid.Parse("0195efb8-7c80-76b3-bb86-ae9dfd74bca2"), LanguageCode = "nno", Type = nameof(Area), Translations = { { "Name", "Fullmakter for forretningsfører" }, { "Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til fullmakter for forretningsfører." } } }
            };

            // Upsert Areas
            foreach (var area in data)
            {
                var existingArea = await db.Areas.FirstOrDefaultAsync(a => a.Id == area.Id, cancellationToken);
                if (existingArea == null)
                {
                    db.Areas.Add(area);
                }
                else
                {
                    existingArea.Urn = area.Urn;
                    existingArea.Name = area.Name;
                    existingArea.Description = area.Description;
                    existingArea.IconUrl = area.IconUrl;
                    existingArea.GroupId = area.GroupId;
                }
            }

            // Upsert TranslationEntryLists
            foreach (var translation in translations.SelectMany(t => t.SingleEntries()))
            {
                await translationService.UpsertTranslationAsync(translation);
            }

            var result = await db.SaveChangesAsync(AuditValues, cancellationToken);
        }
    }
}
