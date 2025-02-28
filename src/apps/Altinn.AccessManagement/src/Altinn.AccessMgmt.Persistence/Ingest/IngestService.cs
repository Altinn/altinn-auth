using System.Text.Json;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Repo.Ingest.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

//// using Altinn.AccessMgmt.Repo.Ingest.RagnhildModel;

namespace Altinn.AccessMgmt.Repo.Ingest;

/// <summary>
/// Json Ingest Factory
/// </summary>
public class IngestService
{
    /// <summary>
    /// Configuration
    /// </summary>
    private readonly IOptions<AccessMgmtPersistenceOptions> options;

    private readonly IProviderRepository providerService;
    private readonly IAreaRepository areaService;
    private readonly IAreaGroupRepository areaGroupService;
    private readonly IEntityTypeRepository entityTypeService;
    private readonly IEntityVariantRepository entityVariantService;
    private readonly IEntityVariantRoleRepository entityVariantRoleService;
    private readonly IPackageRepository packageService;
    private readonly IRoleRepository roleService;
    private readonly IRoleMapRepository roleMapService;
    private readonly IRolePackageRepository rolePackageService;

    /// <summary>
    /// IngestService
    /// </summary>
    /// <param name="options">DbAccessConfig</param>
    /// <param name="providerService">IProviderRepository</param>
    /// <param name="areaService">IAreaRepository</param>
    /// <param name="areaGroupService">IAreaGroupRepository</param>
    /// <param name="entityTypeService">IEntityTypeRepository</param>
    /// <param name="entityVariantService">IEntityVariantRepository</param>
    /// <param name="entityVariantRoleService">IEntityVariantRoleRepository</param>
    /// <param name="packageService">IPackageRepository</param>
    /// <param name="roleService">IRoleRepository</param>
    /// <param name="roleMapService">IRoleMapRepository</param>
    /// <param name="rolePackageService">IRolePackageRepository</param>
    public IngestService(
        IOptions<AccessMgmtPersistenceOptions> options,
        IProviderRepository providerService,
        IAreaRepository areaService,
        IAreaGroupRepository areaGroupService,
        IEntityTypeRepository entityTypeService,
        IEntityVariantRepository entityVariantService,
        IEntityVariantRoleRepository entityVariantRoleService,
        IPackageRepository packageService,
        IRoleRepository roleService,
        IRoleMapRepository roleMapService,
        IRolePackageRepository rolePackageService
        )
    {
        this.options = options;
        this.providerService = providerService;
        this.areaService = areaService;
        this.areaGroupService = areaGroupService;
        this.entityTypeService = entityTypeService;
        this.entityVariantService = entityVariantService;
        this.entityVariantRoleService = entityVariantRoleService;
        this.packageService = packageService;
        this.roleService = roleService;
        this.roleMapService = roleMapService;
        this.rolePackageService = rolePackageService;
    }


    public async Task IngestProvider()
    {
        var providers = new List<Provider>();
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Arbeids- og velferdsetaten (NAV)", RefId = "9baceae9-d600-41f0-952a-9e367e032fc8" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Arbeidstilsynet", RefId = "aa802807-6355-4767-930a-4ee22d02d1c4" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Avinor AS", RefId = "901a46e3-99ce-43bb-bdda-352fad45078f" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Barne-, ungdoms- og familiedirektoratet", RefId = "2f41c948-1da8-4784-802c-534dc3a2906b" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Brønnøysundregistrene", RefId = "323f9ff9-11ad-4af3-8ccb-0db7bdb71d75" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Datatilsynet", RefId = "9f3a7511-a578-4a40-beb7-eab69beb55a3" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Det norske veritas certification AS", RefId = "dac9eaf4-d5b9-4c14-a6f2-ab0c5698c930" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Digdir", RefId = "73dfe32a-8f21-465c-9242-40d82e61f320" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Direktoratet for byggkvalitet", RefId = "50b5e2c3-840c-4462-b140-8865740c0121" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Direktoratet for e-helse", RefId = "20e58977-d65b-40a6-9b14-53152b829f01" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Direktoratet for høyere utdanning og kompetanse", RefId = "b2199137-b758-4e14-ae17-a685d3c357fe" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Direktoratet for medisinske produkter", RefId = "416a2343-5f3e-4623-9ef7-5c691d86e60e" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Direktoratet for mineralforvaltning med Bergmesteren for Svalbard", RefId = "a3cb52af-03ce-4091-a165-b1fad19e3c61" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Direktoratet for samfunnssikkerhet og beredskap (DSB)", RefId = "d1841fa8-94f3-4e29-a9a6-d4f62c309f69" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Direktoratet for strålevern og atomsikkerhet", RefId = "81750d91-e798-4821-b6a2-eac307462f0e" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Domstolene", RefId = "73a50496-111b-4aab-8888-a41089c400c3" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Energidepartementet", RefId = "7e640f81-25d4-44ad-af1c-3e24ba603d49" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Enova", RefId = "2e7c7cfc-4c60-4588-a1d0-a9df5b0b7671" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Fellesordningen for AFP", RefId = "f3919d0d-d0bc-4938-ae82-24b3bcc1843d" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Finanstilsynet", RefId = "5a11460c-815a-4754-ae72-4fd09d04e378" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Fiskeridirektoratet", RefId = "2f91e952-3b21-4891-a011-07d466e7fe4f" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Folkehelseinstituttet", RefId = "e2ae1015-fd67-4275-96f9-462d63d20f4f" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Forsvaret", RefId = "66af2a9f-bf34-4337-8dc0-a6163736d18b" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Fylkeskommunene", RefId = "9a3cbf4b-ce72-4448-a4cb-403ed6366e81" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Garantikassen for fiskere", RefId = "1fc49704-648d-4727-a68b-eb7216a989ce" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Helsedirektoratet", RefId = "0d14ff8c-ebda-4852-9cd3-e300b93e3a08" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Helsedirektoratet (godkjenning av utenlandske yrkeskvalifikasjoner)", RefId = "e60a0cf3-0f49-4fa4-8f0d-7280ed83b25c" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Høgskolen i Bergen", RefId = "253d92ef-46df-47fb-990b-3a7595c0d593" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Høyesterett", RefId = "25a5a805-739a-4ae2-9c3d-98b4d0455210" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Husbanken", RefId = "6716a265-2fee-452b-b5d3-efaea823e9f5" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Kartverket", RefId = "64d8279b-9d94-44d9-a533-e759db58f8e0" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Klima- og miljødepartementet", RefId = "6ea6ba38-ec86-45d8-825c-918116e89789" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Kommunene", RefId = "7e17dc9d-7f28-45f7-a086-402f677e699b" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Konkurransetilsynet", RefId = "0ec9ee75-a959-4b2a-b25a-4c9b263b7f1d" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Kultur- og likestillingsdepartementet", RefId = "03d8a31a-558e-4f3b-8e4c-849650e609c3" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Kulturdirektoratet", RefId = "a2c94798-0ebc-43f9-8ebf-98e58d55e0df" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Kystverket", RefId = "42d702cd-a3e4-496a-8187-660ae77428de" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Landbruks- og matdepartementet", RefId = "ac9d20f5-57b2-4845-9286-141c81e01985" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Landbruksdirektoratet", RefId = "d78d7fe0-c354-412a-9914-35b4543549b0" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Lotteri- og stiftelsestilsynet", RefId = "5612ca66-e5b1-4972-8e91-b8722c8408c5" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Luftfartstilsynet", RefId = "b26127f4-2689-4b75-be84-490c5b9dc8ea" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Mattilsynet", RefId = "76920c46-3137-4e1f-854d-c639aebdc05b" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Medietilsynet", RefId = "93d981ec-60ed-4e57-8ec3-dc73cb99e233" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Mesterbrevnemnda ", RefId = "ab721d0e-3587-4354-a1e2-93eefe788c80" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Miljødirektoratet", RefId = "ccc4cd1b-335e-4987-8b50-af2b04a83c42" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Nærings- og fiskeridepartementet", RefId = "3fcf54c3-91ed-4b9b-935b-ec5751f34cb6" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Nasjonal kommunikasjons­myndighet (Nkom)", RefId = "8f38238d-1f77-4c67-a1e4-032bdde81013" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Nasjonal sikkerhetsmyndighet (NSM)", RefId = "0b1ac5d5-83d1-4c41-9414-6fee4d4327b6" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Nasjonalbiblioteket", RefId = "11bc7d28-84e2-4860-9a90-d5f3671f15e4" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Norges geologiske undersøkelse", RefId = "15c8b665-22ec-411b-844e-b9289aec5360" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Norges Handelshøyskole", RefId = "2718b5d2-20be-4065-8015-ebb6c75bd23e" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Norges sjømatråd", RefId = "dc792dcd-89ca-4a48-9f44-b44e80eab4c3" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Norges vassdrags- og energidirektorat (NVE)", RefId = "8c998624-390a-4651-830c-13acea6141cf" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Norsk pasientskadeerstatning", RefId = "0ddf8040-b602-494a-a9ad-820e9472fa8f" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Norsk rikskringkasting AS", RefId = "e2badfb8-090f-4baf-a550-eacd09bcb043" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Oljedirektoratet", RefId = "9f3bf332-2c9e-45b3-9b3a-1cea795dfca0" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Patentstyret", RefId = "f6e701a1-cfaf-4c3f-9829-7fa8d30ae78b" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Pensjonstrygden for sjømenn", RefId = "34cc0a83-b9f7-42d0-ae1a-7417430113ad" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Petroleumstilsynet", RefId = "6bf70088-4dd4-48c2-8157-8b5b1b0fadbb" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Politiet", RefId = "3fa85269-37f8-42d6-992c-b8fd4f6319e5" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Reisegarantifondet", RefId = "fda4dda1-cfff-42e9-bb77-115dee49c057" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "REK - Regionale komiteer for medisinsk og helsefaglig forskningsetikk", RefId = "b89f7e5d-d447-4c42-9709-e1cc23d4fc36" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Samferdselsdepartementet", RefId = "ebb9f8a2-60c9-4c5d-81bc-0b945d039079" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Sjøfartsdirektoratet", RefId = "9810c69f-3b3e-4fdb-9753-fed8d10eab01" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Skatteetaten", RefId = "ac991c6f-e404-4c8a-82ab-94c2f772cd00" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Skipsregistrene", RefId = "a5bf45da-e870-49dc-adde-65ce208063b5" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Statens arbeidsmiljøinstitutt", RefId = "4853dd4a-8d67-4621-97df-d27753094076" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Statens jernbanetilsyn", RefId = "84350c52-76e6-47f4-bb57-47d16c6dd0ff" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Statens pensjonskasse", RefId = "8d92eda2-e2a9-4ad0-b55d-9dd86b0e155c" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Statens sivilrettsforvaltning", RefId = "f52835a4-61a6-4088-8992-3d84216c52bf" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Statens vegvesen", RefId = "d1e85a31-38c3-45a4-bbb3-f5ab42af32f6" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Statistisk sentralbyrå", RefId = "c719140a-9066-47af-95b3-cdaf160a0024" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Statsforvalteren", RefId = "5e8b8e4c-21e8-4fbb-bedd-944e1ee3620f" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Sysselmannen på Svalbard", RefId = "a4fcc22c-a2f6-48c6-8653-9c238f012965" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Tilsynsrådet for advokatvirksomhet", RefId = "5db877e5-71ee-48d1-a978-c38c4d9ee93d" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Tolletaten", RefId = "f0092d49-87f8-4f9e-a381-b4d0cdf968da" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Utdanningsdirektoratet", RefId = "0dd397c3-1755-4551-8498-a94e6eae311f" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Utenriksdepartementet", RefId = "fe0a796d-a63d-462d-b45c-b7e9b0f90d0c" });
        providers.Add(new Provider() { Id = Guid.NewGuid(), Name = "Utlendingsdirektoratet", RefId = "3cadedcb-d412-47b7-b9d4-77d8fcd6bbb6" });

        // Løsning 1:
        //var translations = new Dictionary<Guid, Dictionary<string, Dictionary<string, string>>>
        //{
        //    {
        //        new List<Provider>() {
        //        providers[0].Id, new Dictionary<string, Dictionary<string, string>>
        //        {
        //            {
        //                "Name", new Dictionary<string, string>
        //                {
        //                    { "nob", "Digdir" },
        //                    { "eng", "Digdir" }
        //                }
        //            }
        //        }
        //    }
        //};

        //// Løsning 2
        //var translated = new Dictionary<string, List<Provider>>
        //{
        //    {
        //        "eng",
        //            new Provider() { Id = Guid.NewGuid(), Name = "Digdir", RefId = "00000000" },
        //            new Provider() { Id = Guid.NewGuid(), Name = "Digdir", RefId = "00000000" },
        //            new Provider() { Id = Guid.NewGuid(), Name = "Digdir", RefId = "00000000" }
        //        }
        //    }
        //};

        foreach (var item in providers)
        {
            Console.WriteLine(item.Name);
            await providerService.Upsert(item);
        }
    }

    public async Task IngestEntityType()
    {
        var entityTypes = new List<EntityType>();
        entityTypes.Add(new EntityType() { Id = Guid.NewGuid(), Name = "Organisasjon", ProviderId = Guid.Parse("db4d3dca-8f70-4353-b408-a495677087b3") });
        entityTypes.Add(new EntityType() { Id = Guid.NewGuid(), Name = "Person", ProviderId = Guid.Parse("db4d3dca-8f70-4353-b408-a495677087b3") });

        foreach (var item in entityTypes)
        {
            await entityTypeService.Upsert(item);
        }
    }

    public async Task IngestEntityVariant()
    {
        var entityVariants = new List<EntityVariant>();
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "SAM", Description = "Tingsrettslig sameie" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "VPFO", Description = "Verdipapirfond" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "UTLA", Description = "Utenlandsk enhet" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "BO", Description = "Andre bo" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "AS", Description = "Aksjeselskap" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "PK", Description = "Pensjonskasse" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "PERS", Description = "Andre enkeltpersoner som registreres i tilknyttet register" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "EOFG", Description = "Europeisk økonomisk foretaksgruppe" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "SE", Description = "Europeisk selskap" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "TVAM", Description = "Tvangsregistrert for MVA" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("bfe09e70-e868-44b3-8d81-dfe0e13e058a"), Name = "Person", Description = "Person" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "GFS", Description = "Gjensidig forsikringsselskap" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "FYLK", Description = "Fylkeskommune" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "IKJP", Description = "Andre ikke-juridiske personer" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "NUF", Description = "Norskregistrert utenlandsk foretak" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "ANS", Description = "Ansvarlig selskap med solidarisk ansvar" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "KS", Description = "Kommandittselskap" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "SÆR", Description = "Annet foretak iflg. særskilt lov" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "IKS", Description = "Interkommunalt selskap" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "STI", Description = "Stiftelse" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "BBL", Description = "Boligbyggelag" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "KTRF", Description = "Kontorfellesskap" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "ANNA", Description = "Annen juridisk person" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "SA", Description = "Samvirkeforetak" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "ADOS", Description = "Administrativ enhet - offentlig sektor" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "KF", Description = "Kommunalt foretak" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "AAFY", Description = "Underenhet til ikke-næringsdrivende" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "DA", Description = "Ansvarlig selskap med delt ansvar" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "OPMV", Description = "Særskilt oppdelt enhet, jf. mval. § 2-2" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "ORGL", Description = "Organisasjonsledd" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "STAT", Description = "Staten" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "SF", Description = "Statsforetak" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "PRE", Description = "Partrederi" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "BRL", Description = "Borettslag" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "KOMM", Description = "Kommune" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "FLI", Description = "Forening/lag/innretning" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "SPA", Description = "Sparebank" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "ASA", Description = "Allmennaksjeselskap" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "ESEK", Description = "Eierseksjonssameie" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "ENK", Description = "Enkeltpersonforetak" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "FKF", Description = "Fylkeskommunalt foretak" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "KIRK", Description = "Den norske kirke" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "BEDR", Description = "Underenhet til næringsdrivende og offentlig forvaltning" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "KBO", Description = "Konkursbo" });
        entityVariants.Add(new EntityVariant() { Id = Guid.NewGuid(), TypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), Name = "BA", Description = "Selskap med begrenset ansvar" });

        foreach (var item in entityVariants)
        {
            await entityVariantService.Upsert(item);
        }
    }

    public async Task IngestRole()
    {
        var roles = new List<Role>();
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Administrativ enhet - offentlig sektor", Code = "ADOS", Description = "Administrativ enhet - offentlig sektor", Urn = "brreg:role:ados" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Nestleder", Code = "NEST", Description = "Styremedlem som opptrer som styreleder ved leders fravær", Urn = "brreg:role:nest" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Inngår i kontorfellesskap", Code = "KTRF", Description = "Inngår i kontorfellesskap", Urn = "brreg:role:ktrf" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Organisasjonsledd i offentlig sektor", Code = "ORGL", Description = "Organisasjonsledd i offentlig sektor", Urn = "brreg:role:orgl" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Særskilt oppdelt enhet", Code = "OPMV", Description = "Særskilt oppdelt enhet", Urn = "brreg:role:opmv" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("73dfe32a-8f21-465c-9242-40d82e61f320"), Name = "Klientadministrator", Code = "KLA", Description = "Gir mulighet til å administrere tilgang til tjenester videre til ansatte på vegne av deres kunder", Urn = "digdir:role:kla" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Daglig leder", Code = "DAGL", Description = "Fysisk- eller juridisk person som har ansvaret for den daglige driften i en virksomhet", Urn = "brreg:role:dagl" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("73dfe32a-8f21-465c-9242-40d82e61f320"), Name = "Tilgangsstyrer", Code = "TS", Description = "Gir mulighet til å gi videre tilganger for virksomheten som man selv har mottatt", Urn = "digdir:role:ts" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Deltaker delt ansvar", Code = "DTPR", Description = "Fysisk- eller juridisk person som har personlig ansvar for deler av selskapets forpliktelser", Urn = "brreg:role:dtpr" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("73dfe32a-8f21-465c-9242-40d82e61f320"), Name = "Hovedadministrator", Code = "HA", Description = "Gir mulighet til å administrere alle tilganger for virksomheten", Urn = "digdir:role:ha" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Innehaver", Code = "INNH", Description = "Fysisk person som er eier av et enkeltpersonforetak", Urn = "brreg:role:innh" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("73dfe32a-8f21-465c-9242-40d82e61f320"), Name = "Kundeadministrator", Code = "KUA", Description = " Gir mulighet til å administrere tilganger man har mottatt for sine kunder til ansatte i egen virksomheten", Urn = "digdir:role:kua" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Deltaker fullt ansvar", Code = "DTSO", Description = "Fysisk- eller juridisk person som har ubegrenset, personlig ansvar for selskapets forpliktelser", Urn = "brreg:role:dtso" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Varamedlem", Code = "VARA", Description = "Fysisk- eller juridisk person som er stedfortreder for et styremedlem", Urn = "brreg:role:vara" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Observatør", Code = "OBS", Description = "Fysisk person som deltar i styremøter i en virksomhet, men uten stemmerett", Urn = "brreg:role:obs" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Styremedlem", Code = "MEDL", Description = "Fysisk- eller juridisk person som inngår i et styre", Urn = "brreg:role:medl" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Styrets leder", Code = "LEDE", Description = "Fysisk- eller juridisk person som er styremedlem og leder et styre", Urn = "brreg:role:lede" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Den personlige konkursen angår", Code = "KENK", Description = "Den personlige konkursen angår", Urn = "brreg:role:kenk" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Norsk representant for utenlandsk enhet", Code = "REPR", Description = "Fysisk- eller juridisk person som har ansvaret for den daglige driften i Norge", Urn = "brreg:role:repr" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Kontaktperson", Code = "KONT", Description = "Fysisk person som representerer en virksomhet", Urn = "brreg:role:kont" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Kontaktperson NUF", Code = "KNUF", Description = "Fysisk person som representerer en virksomhet - NUF", Urn = "brreg:role:knuf" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Bestyrende reder", Code = "BEST", Description = "Bestyrende reder", Urn = "brreg:role:best" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Eierkommune", Code = "EIKM", Description = "Eierkommune", Urn = "brreg:role:eikm" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Bobestyrer", Code = "BOBE", Description = "Bestyrer av et konkursbo eller dødsbo som er under offentlig skiftebehandling", Urn = "brreg:role:bobe" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Helseforetak", Code = "HLSE", Description = "Helseforetak", Urn = "brreg:role:hlse" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Revisor", Code = "REVI", Description = "Revisor", Urn = "brreg:role:revi" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Forretningsfører", Code = "FFØR", Description = "Forretningsfører", Urn = "brreg:role:ffør" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Komplementar", Code = "KOMP", Description = "Komplementar", Urn = "brreg:role:komp" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Konkursdebitor", Code = "KDEB", Description = "Konkursdebitor", Urn = "brreg:role:kdeb" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Inngår i kirkelig fellesråd", Code = "KIRK", Description = "Inngår i kirkelig fellesråd", Urn = "brreg:role:kirk" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Opplysninger om foretaket i hjemlandet", Code = "HFOR", Description = "Opplysninger om foretaket i hjemlandet", Urn = "brreg:role:hfor" });
        roles.Add(new Role() { Id = Guid.NewGuid(), EntityTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d"), ProviderId = Guid.Parse("323f9ff9-11ad-4af3-8ccb-0db7bdb71d75"), Name = "Regnskapsfører", Code = "REGN", Description = "Regnskapsfører", Urn = "brreg:role:regn" });


        foreach (var item in roles)
        {
            await roleService.Upsert(item);
        }
    }

    public async Task IngestRoleMap()
    {
        var roleMaps = new List<RoleMap>();
        roleMaps.Add(new RoleMap() { Id = Guid.NewGuid(), HasRoleId = Guid.Parse("857bb1fb-fcaa-4a3c-859d-319032626539"), GetRoleId = Guid.Parse("ba1c261c-20ec-44e2-9e0b-4e7cfe9f36e7") });
        roleMaps.Add(new RoleMap() { Id = Guid.NewGuid(), HasRoleId = Guid.Parse("857bb1fb-fcaa-4a3c-859d-319032626539"), GetRoleId = Guid.Parse("6c1fbcb9-609c-4ab8-a048-3be8d7da5a82") });
        roleMaps.Add(new RoleMap() { Id = Guid.NewGuid(), HasRoleId = Guid.Parse("72c336a2-1705-4aef-b220-7f4aa6c0e69d"), GetRoleId = Guid.Parse("6c1fbcb9-609c-4ab8-a048-3be8d7da5a82") });

        foreach (var item in roleMaps)
        {
            await roleMapService.Upsert(item);
        }
    }

    public async Task IngestAreaGroup()
    {
        var areaGroups = new List<AreaGroup>();
        areaGroups.Add(new AreaGroup() { Id = Guid.NewGuid(), Name = "Allment", Description = "Standard gruppe" });
        areaGroups.Add(new AreaGroup() { Id = Guid.NewGuid(), Name = "Bransje", Description = "For bransje grupper" });
        areaGroups.Add(new AreaGroup() { Id = Guid.NewGuid(), Name = "Særskilt", Description = "For de sære tingene" });

        foreach (var item in areaGroups)
        {
            await areaGroupService.Upsert(item);
        }
    }

    public async Task IngestArea()
    {
        var areas = new List<Area>();
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:skatt_avgift_regnskap_og_toll", Name = "Skatt, avgift, regnskap og toll", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til skatt, avgift, regnskap og toll.", IconName = "Money_SackKroner", GroupId = Guid.Parse("7E2A3AF8-08CB-43A9-BDD7-7D5C7E377145") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:personale", Name = "Personale", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til personale.", IconName = "People_PersonGroup", GroupId = Guid.Parse("7E2A3AF8-08CB-43A9-BDD7-7D5C7E377145") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:miljo_ulykke_og_sikkerhet", Name = "Miljø, ulykke og sikkerhet", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til miljø, ulykke og sikkerhet.", IconName = "People_HandHeart", GroupId = Guid.Parse("7E2A3AF8-08CB-43A9-BDD7-7D5C7E377145") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:post_og_arkiv", Name = "Post og arkiv", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til post og arkiv.", IconName = "Interface_EnvelopeClosed", GroupId = Guid.Parse("7E2A3AF8-08CB-43A9-BDD7-7D5C7E377145") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:forhold_ved_virksomheten", Name = "Forhold ved virksomheten", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til forhold ved virksomheten.", IconName = "Workplace_Buildings3", GroupId = Guid.Parse("7E2A3AF8-08CB-43A9-BDD7-7D5C7E377145") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:integrasjoner", Name = "Integrasjoner", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til integrasjoner.", IconName = "Interface_RotateLeft", GroupId = Guid.Parse("7E2A3AF8-08CB-43A9-BDD7-7D5C7E377145") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:administrere_tilganger", Name = "Administrere tilganger", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til administrere tilganger.", IconName = "People_PersonLock", GroupId = Guid.Parse("7E2A3AF8-08CB-43A9-BDD7-7D5C7E377145") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:jordbruk_skogbruk_jakt_fiske_og_akvakultur", Name = "Jordbruk, skogbruk, jakt, fiske og akvakultur", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til jordbruk, skogbruk, jakt, fiske og akvakultur.", IconName = "Nature-and-animals_Plant", GroupId = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:bygg_anlegg_og_eiendom", Name = "Bygg, anlegg og eiendom", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til bygg, anlegg og eiendom.", IconName = "People_HandHouse", GroupId = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:transport_og_lagring", Name = "Transport og lagring", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til transport og lagring.", IconName = "Transportation_Truck", GroupId = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:helse_pleie_omsorg_og_vern", Name = "Helse, pleie, omsorg og vern", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til helse, pleie, omsorg og vern.", IconName = "Wellness_Hospital", GroupId = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:oppvekst_og_utdaning", Name = "Oppvekst og utdaning", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til oppvekst og utdaning.", IconName = "Workplace_Buildings2", GroupId = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:energi_vann_avlop_og_avfall", Name = "Energi, vann,avløp og avfall", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til energi, vann,avløp og avfall.", IconName = "Workplace_TapWater", GroupId = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:industrier", Name = "Industrier", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til industrier.", IconName = "Factory", GroupId = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:kultur_og_frivillighet", Name = "Kultur og frivillighet", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til kultur og frivillighet.", IconName = "Wellness_HeadHeart", GroupId = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:handel_overnatting_og_servering", Name = "Handel, overnatting og servering", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til handel, overnatting og servering.", IconName = "Wellness_TrayFood", GroupId = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:andre_tjenesteytende_naeringer", Name = "Andre tjenesteytende næringer", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til andre tjenesteytende næringer.", IconName = "Workplace_Reception", GroupId = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:fullmakter_for_regnskapsforer", Name = "Fullmakter for regnskapsfører", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for regnskapsfører.", IconName = "Home_Calculator", GroupId = Guid.Parse("554F0321-53B8-4D97-BE12-6A585C507159") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:fullmakter_for_revisor", Name = "Fullmakter for revisor", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for revisor.", IconName = "Files-and-application_FileSearch", GroupId = Guid.Parse("554F0321-53B8-4D97-BE12-6A585C507159") });
        areas.Add(new Area() { Id = Guid.NewGuid(), Urn = "accesspackage:area:fullmakter_for_konkursbo", Name = "Fullmakter for konkursbo", Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for konkursbo.", IconName = "Statistics-and-math_TrendDown", GroupId = Guid.Parse("554F0321-53B8-4D97-BE12-6A585C507159") });

        foreach (var item in areas)
        {
            await areaService.Upsert(item);
        }
    }

    public async Task IngestPackage()
    {
        var packages = new List<Package>();
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:skattnaering", Name = "Skatt næring", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til skatt for næringer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:skattegrunnlag", Name = "Skattegrunnlag", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til innhenting av skattegrunnlag. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:merverdiavgift", Name = "Merverdiavgift", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til merverdiavgift. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:motorvognavgift", Name = "Motorvognavgifter", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til motorvognavgifter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:regnskapokonomirapport", Name = "Regnskap og økonomirapportering", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til regnskap og øknomirapportering som ikke tilhører skatt og merverdiavgift. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:kravogutlegg", Name = "Krav, betalinger og utlegg", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til krav og utlegg. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:reviorattesterer", Name = "Revisorattesterer", Description = "Denne fullmakten gir tilgang til alle tjenester som krever revisorattestering. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:saeravgifter", Name = "Særavgifter", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til særavgifter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:kredittogoppgjoer", Name = "Kreditt- og oppgjørsordninger", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til kreditt- og oppgjørsordninger. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:toll", Name = "Toll", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til toll og fortolling. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:ansettelsesforhold", Name = "Ansettelsesforhold", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til ansettelsesforhold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:lonn", Name = "Lønn", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til lønn og honorar. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:aordning", Name = "A-ordningen", Description = "Denne tilgangspakken gir fullmakter til tjenester som inngår i A-ordningen. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  OBS! Vær oppmerksompå at tilgangspakken inneholder fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal gis som enkeltrettigheter." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:pensjon", Name = "Pensjon", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til pensjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:sykefravaer", Name = "Sykefravær", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sykefravær. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:permisjon", Name = "Permisjon", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til permisjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:renovasjon", Name = "Renovasjon", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til renovasjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:miljoryddingmiljorensingoglignende", Name = "Miljørydding, miljørensing og lignende", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til miljørydding, miljørensing og lignende. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:baerekraft", Name = "Bærekraft", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til tiltak og rapportering på bærekraft. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:sikkerhetoginternkontroll", Name = "Sikkerhet og internkontroll", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sikkerhet og internkontroll. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:ulykke", Name = "Ulykke", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til ulykke. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:yrkesskade", Name = "Yrkesskade", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til yrkesskade. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:ordinaerposttilvirksomheten", Name = "Ordinær post til virksomheten", Description = "Denne fullmakten gir tilgang til all mottatt post som ikke innholder sensitiv/taushetsbelagt informasjon som sendes virksomheten. Fullmakten gis normalt til de i virksomheten som håndterer mottak av post. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:posttilvirksomhetenmedtaushetsbelagtinnhold", Name = "Post til virksomheten med taushetsbelagt innhold", Description = "Denne fullmakten gir tilgang til all mottatt post som innholder sensitiv/taushetsbelagt informasjon som sendes virksomheten. Fullmakten gis normalt til de i virksomheten som håndterer mottak av post som har taushetsbelagt innhold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:generellehelfotjenester", Name = "Generelle helfotjenester", Description = "Denne fullmakten gir tilgang til ordinære tjenester knyttet til virksomhetens dialog med Helfo der bruker kan få tilgang til personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:helfosearligkategori", Name = "Helfotjenester med personopplysninger av særlig kategori", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til virksomhetens dialog med Helfo der bruker kan få tilgang til personopplysninger av særlig kategori. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir" });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:startedriveendreaviklevirksomhet", Name = "Starte, endre og avvikle virksomhet", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til å starte, endre og avvikle en virksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:aksjerogeierforhold", Name = "Aksjer og eierforhold", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til aksjer og eierforhold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:attester", Name = "Attester", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til attestering av virksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:dokumentbaserttilsyn", Name = "Dokumentbasert tilsyn", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til dokumentbaserte tilsyn. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:infrastruktur", Name = "Infrastruktur", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til virksomhetens infrastruktur. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:patentvaremerkedesign", Name = "Patent, varemerke og design", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til patent, varemerke og design. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:tilskuddstotteerstatning", Name = "Tilskudd, støtte og erstatning", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til å søke tilskudd, støtte og erstatning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:minesiderkommune", Name = "Mine sider hos kommunen", Description = "Denne fullmakten gir generell tilgang til tjenester av typen “mine side” tjenester hos kommuner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:politidomstol", Name = "Politi og domstol", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til virksomhetens dialog om juridiske forhold med politi og jusitsmyndigheter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:rapporteringstatistikk", Name = "Rapportering av statistikk", Description = "Denne fullmakten gir tilgang til alle pålagte rapportering av statistikk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:forskning", Name = "Forskning", Description = "Denne fullmakten gir tilgang til tjenester knyttet til forskning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:eksplisitt", Name = "Eksplisitt tjenestedelegering", Description = "Denne fullmakten er ikke delegerbar, og er ikke knyttet til noen roller i ENhetsregisteret. Tilgang til tjenester knyttet til denne pakken kan gis av Hovedadministrator gjennom enkeltrettighetsdelegering." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:folkeregister", Name = "Folkeregister", Description = "Denne tilgangspakken gir fullmakt til tjenester som en virksomhet kan ha mot folkeregister. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:programmeringsgrensesnitt", Name = "Programmeringsgrensesnitt (API)", Description = "Denne tilgangspakken gir fullmakter til å administrere tilgang til data og programmeringsgrensenitt (API) som benytter Maskinporten eller tilsvarende løsninger for APIsikring. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:maskinlesbarehendelser", Name = "Maskinlesbare hendelser", Description = "Denne tilgangspakken gir fullmakter til å administrere tilgang til maskinlesbare hendelser. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:programmeringsgrensesnittNUF", Name = "Programmeringsgrensesnitt NUF (API)", Description = "Denne tilgangspakken gir fullmakter til å administrere tilgang til data og programmeringsgrensenitt (API) som benytter Maskinporten eller tilsvarende løsninger for APIsikring på vegne av norskregistrerte utenlandske foretak (NUF). Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:klientadminstrasjonforregnskapsforerogtevisor", Name = "Klientadministrasjon for regnskapsfører og revisor", Description = "Denne tilgangspakken gir bruker mulighet til å administrere tilgang til tjenester det er naturlig at regnskapsfører eller revisor utfører. Bruker kan administrere tilgang til tjenestene til ansatte hos regnskapsfører eller revisor på vegne av deres kunder." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:tilgangsstyring", Name = "Tilgangsstyring", Description = "Denne tilgangspakken gir bruker mulighet til å gi videre tilganger for virksomheten som man selv innehar." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:hovedadministrator", Name = "Hovedadministrator", Description = "Denne tilgangspakken gir bruker mulighet til å administrere alle tilganger for virksomheten." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:kundeadministrator", Name = "Kundeadministrator", Description = "Denne tilgangspakken gir bruker mulighet til å administrere tilganger man har mottatt for sine kunder til ansatte i egen virksomheten. " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:jordbruk", Name = "Jordbruk", Description = "Denne tilgangspakken gir tilgang til tjenester knyttet til jordbruk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:dyrehold", Name = "Dyrehold", Description = "Denne tilgangspakken gir tilgang til tjenester knyttet til dyrehold. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:reindrift", Name = "Reindrift", Description = "Denne tilgangspakken gir tilgang til tjenester knyttet til reindrift. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:jaktogviltstell", Name = "Jakt og viltstell", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til jakt og viltstell. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:skogbruk", Name = "Skogbruk", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til skogbruk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:fiske", Name = "Fiske", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til fiske. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:akvakultur", Name = "Akvakultur", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til akvakultur og fiskeoppdrett. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:byggesoknad", Name = "Byggesøknad", Description = "Denne tilgangspakken gir fullmakter til tjenester som ansvarlig søker/tiltakshaver trenger, for eksempel byggesøknader, direkte signerte erklæringer, nabovarsel og eiendomssak. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:plansak", Name = "Plansak", Description = "Denne tilgangspakken gir fullmakter til tjenester som forslagsstiller/ plankonsulent trenger, for eksempel varsel om planopppstart og høring og offentlig ettersyn. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:mottanaboogplanvarsel", Name = "Motta nabo- og planvarsel", Description = "Denne tilgangspakken gir fullmakter til tjenester til å lese og svare på varsel om plan-/byggesak. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:oppforingbygganlegg", Name = "Oppføring av bygg og anlegg", Description = "Denne tilgangspakken gir fullmakter til tjenester relatert til oppføring av bygninger og annlegg unntatt plan og byggesaksbehandling. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:kjopogsalgeiendom", Name = "Kjøp og salg av eiendom", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til kjøp og salg av eiendom. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:utleieeiendom", Name = "Utleie av eiendom", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til utleie av eiendom. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:eiendomsmegler", Name = "Eiendomsmegler", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til omsetning og drift av fast eiendom på oppdrag, som eiendomsmegling og eiendomsforvaltning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:veitransport", Name = "Veitransport", Description = "Denne fullmakten gir tilgang til tjenester knyttet til person- og godstransport langs veinettet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:transportiror", Name = "Transport i rør", Description = "Denne fullmakten gir tilgang til tjenester knyttet til transport i rør. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:sjofart", Name = "Sjøfart", Description = "Denne fullmakten gir tilgang til tjenester knyttet til skipsarbeidstakere og fartøy til sjøs. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:lufttransport", Name = "Lufttransport", Description = "Denne fullmakten gir tilgang til tjenester knyttet til luftfartøy og romfartøy. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:jernbanetransport", Name = "Jernbanetransport", Description = "Denne fullmakten gir tilgang til tjenester knyttet til jernbane, inkludert trikk, T-bane og sporvogn. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:lagringogandretjenestertilknyttettransport", Name = "Lagring og andre tjenester tilknyttet transport", Description = "Denne fullmakten gir tilgang til tjenester knyttet til lagring og hjelpetjenester i forbindelse med transport, samt post- og kurervirksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:kommuneoverlege", Name = "Kommuneoverlege", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sykehus, lege, tannlege og hjemmesykepleie,fysioterapi, ambulanse og lignende, som er relevant for kommuneleger. Denne fullmakten kan gi bruker tilgang til personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:helsetjenesterpersonopplysingersaerligkategori", Name = "Helsetjenester med personopplysninger av særlig kategori", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sykehus, lege, tannlege og hjemmesykepleie,fysioterapi, ambulanse og lignende, som er av særlig kategori. Denne fullmakten kan gi bruker tilgang til sensitive personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:helsetjenester", Name = "Helsetjenester", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sykehus, lege, tannlege og hjemmesykepleie,fysioterapi, ambulanse og lignende. Denne fullmakten kan gi bruker tilgang til personopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:pleieomsorgstjenesteriinstitusjon", Name = "Pleie- og omsorgstjenester i institusjon", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til pleie og omsorgstilbud i institursjon. Dette er tjenester som tilbyr institusjonsopphold kombinert med sykepleie, tilsyn eller annen form for pleie alt etter hva som kreves av beboerne. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:sosialeomsorgstjenesterutenbotilbudogflyktningemottak", Name = "Sosiale omsorgstjenester uten botilbud og flyktningemottak", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sosiale omsorgstjeneser uten botilbud for eldre, funksjonshemmede og rusmisbrukere samt flykningemottak, og tjenester relatert til arbeidstrening og andre sosiale tjenester, f eks i regi av velferdsorganisasjoner. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:familievern", Name = "Familievern", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til familievern. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. OBS! Vær oppmerksom på at tilgangspakken inneholder fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal gis som enkeltrettigheter." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:barnevern", Name = "Barnevern", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til barnevern. Denne fullmakten kan gi bruker tilgang til helseopplysninger om personer det rapporteres om. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir. OBS! Vær oppmerksom på at tilgangspakken inneholder fullmakter som kan ha sensitiv karakter. Vurder om fullmaktene skal gis som enkeltrettigheter. " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:godkjenningavpersonell", Name = "Godkjenning av personell", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til å søke om godkjenning til enkeltpersoner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:godkjenningavutdanningsvirksomhet", Name = "Godkjenning av utdanningsvirksomhet", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til å søke om godkjenning av utdanningsvirksomheter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:hoyereutdanningoghoyereyrkesfagligutdanning", Name = "Høyere utdanning og høyere yrkesfaglig utdanning", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til høyere utdanning og høyere yrkesfaglig utdanning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:sfoleder", Name = "SFO-leder", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til førskole og fritidsordning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:pptleder", Name = "PPT-leder", Description = "Denne fullmakten gir tilgang til tjenester innen drift av Pedagogisk-psykologisk tjeneste (PPT) som PPT-leder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:opplaeringskontorleder", Name = "Opplæringskontorleder", Description = "Denne fullmakten gir tilgang til tjenester innen drift av opplæringskontor som opplæringskontorleder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:skoleleder", Name = "Skoleleder", Description = "Denne fullmakten gir tilgang til tjenester innen drift av skole som skoleleder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:skoleeier", Name = "Skoleeier", Description = "Denne fullmakten gir tilgang til tjenester innen drift av skole som skoleeier er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:statsforvalterskoleogopplearing", Name = "Statsforvalter - skole og opplæring", Description = "Denne fullmakten gir tilgang til tjenester for statsforvalter knyttet til skole- og opplæringssektor, herunder fagopplæring og voksenopplæring." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:statsforvalterbarnehage", Name = "Statsforvalter - barnehage", Description = "Denne fullmakten gir tilgang til tjenester for statsforvalter knyttet til barnehagesektor." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:barnehagemyndighet", Name = "Barnehagemyndighet", Description = "Denne fullmakten gir tilgang til tjenester innen drift av barnehage som barnehagemyndighet er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:barnehageleder", Name = "Barnehageleder", Description = "Denne fullmakten gir tilgang til tjenester innen drift av barnehage som barnehageleder er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:barnehageeier", Name = "Barnehageeier", Description = "Denne fullmakten gir tilgang til tjenester innen drift av barnehage som barnehageeier er ansvarlig for. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.   " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:elektrisitetprodusereoverforedistrubere", Name = "Elektrisitet - produsere, overføre og distribuere", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til elektrisitet: produsere, overføre og distribuere. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:dampvarmtvann", Name = "Damp- og varmtvann", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til damp- og varmtvann. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:vannkilderensedistrubere", Name = "Vann - ta ut fra kilde, rense og distribuere", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til vann: ta ut fra kilde, rense og distribuere. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:samlebehandleavlopsvann", Name = "Samle opp og behandle avløpsvann", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til samle opp og behandle avløpsvann. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:avfallbehandlegjenvinne", Name = "Avfall - samle inn, behandle, bruke og gjenvinne", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til avfall: samle inn, behandle bruke og gjenvinne. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:miljoryddingrensing", Name = "Miljørydding - rensing og lignende virksomhet", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til miljøryddng, -rensing og lignende virksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:utvinningraoljenaturgasskull", Name = "Utvinning av råolje, naturgass og kull", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til utvinning av råolje, naturgass og kull. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.   " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:naeringsmidlerdrikkevarerogtobakk", Name = "Næringsmidler, drikkevarer og tobakk", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med næringsmidler, drikkevarer og tobakk. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:tekstilerklaerlaervarer", Name = "Tekstiler, klær og lærvarer", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med tekstiler, klær og lærvarer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:trelasttrevarerpapirvarer", Name = "Trelast, trevarer og papirvarer", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med trelast, trevarer og papirvarer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:trykkerierreproduksjonopptak", Name = "Trykkerier og reproduksjon av innspilte opptak", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med trykkerier og reproduksjon av innspilte opptak. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:oljeraffineringkjemiskfarmasoytiskindustri", Name = "Oljeraffinering, kjemisk og farmasøytisk industri", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med oljeraffinering, kjemisk og farmasøytisk industri. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:gummiplastogikkemetallholdigemineralprodukter", Name = "Gummi, plast og ikke-metallholdige mineralprodukter", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med gummi, plast og ikke-metallholdige mineralprodukter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:metallerogmineraler", Name = "Metaller og mineraler", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med metaller og mineraler. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:metallvarerelektriskutstyrogmaskiner", Name = "Metallvarer elektrisk utstyr og maskiner", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med metallvarer, elektrisk utstyr og maskiner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:verftogandretransportmidler", Name = "Verft og andre transportmidler", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med verft og andre transportmidler. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:moblerogannenindustri", Name = "Møbler og annen industri", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med møbler og annen industri. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:reparasjonoginstallasjonavmaskinerogutstyr", Name = "Reparasjon og installasjon av maskiner og utstyr", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med reparasjon og installasjon av maskiner og utstyr. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:bergverk", Name = "Bergverk", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til industri i forbindelse med bergverk og tilhørende tjenester til bergverksdrift og utvinning. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.   " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:kunstogunderholdning", Name = "Kunst og underholdning", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til kunstnerisk og underholdningsaktiviteter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:bibliotekermuseerarkiverogannenkultur", Name = "Biblioteker, museer, arkiver og annen kultur", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til biblioteker, museer, arkiver, og annen kultur som botaniske og zoologiske hager, og drift av naturfenomener av historisk, kulturell eller undervisningsmessig interesse (f.eks. verdenskulturarv mv.). Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:lotteriogspill", Name = "Lotteri og spill", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til lotteri og spill, som f eks kasinoer, bingohaller og videospillhaller samt spillevirksomhet som f.eks. lotterier og veddemål som inngås utenfor banen. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:sportsogfritid", Name = "Sport og fritid", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til sports- og fritidsaktiviteter. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:fornoyelser", Name = "Fornøyelser", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til drift av fornøyelsesetablissementer. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:politikk", Name = "Politikk", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til aktiviteter i forbindelse med politisk arbeid. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.   " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:varehandel", Name = "Varehandel", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til varehandel, inkludert engros- og detaljhandel, import og eksport, og salg og reparasjon av motorvogner. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:overnatting", Name = "Overnatting", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til overnattingsvirksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir." });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:servering", Name = "Servering", Description = "Denne tilgangspakken gir fullmakter til tjenester knyttet til serveringsvirksomhet. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.   " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:postogtelekommunikasjon", Name = "Post- og telekommunikasjon", Description = "Denne fullmakten gir tilgang til tjenester knyttet til post og telekommunikasjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:informasjonogkommunikasjon", Name = "Informasjon og kommunikasjon", Description = "Denne fullmakten gir tilgang til tjenester knyttet til informasjon og kommunikasjon. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:finansieringogforsikring", Name = "Finansiering og forsikring", Description = "Denne fullmakten gir tilgang til tjenester knyttet til finansiering og forsikring. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:annentjenesteyting", Name = "Annen tjenesteyting", Description = "Denne fullmakten gir tilgang til tjenester knyttet til annen tjenesteyting som f eks organisasjoner og foreninger, reparasjon av datamaskiner og varer til personlig bruk og husholdningsbruk og en rekke personlige tjenester som ikke er nevnt annet sted. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.   " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:regnskapsforermedsigneringsrettighet", Name = "Regnskapsfører med signeringsrettighet", Description = "Denne fullmakten gir tilgang til regnskapfører å kunne signere på vegne av kunden for alle tjenester som krever signeringsrett. Dette er tjenester som man har vurdert det som naturlig at en regnskapsfører utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte regnskapsførere. Fullmakt hos regnskapfører oppstår når kunden registrerer regnskapsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:regnskapsforerutensigneringsrettighet", Name = "Regnskapsfører uten signeringsrettighet", Description = "Denne fullmakten gir tilgang til å kunne utføre alle tjenester som ikke krever signeringsrett. Dette er tjenester som man har vurdert det som naturlig at en regnskapsfører utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte regnskapsførere. Fullmakt hos regnskapfører oppstår når kunden registrerer regnskapsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:regnskapsforerlonn", Name = "Regnskapsfører lønn", Description = "Denne fullmakten gir tilgang til regnskapsfører å rapportere lønn for sin kunde. Dette er tjenester som man har vurdert det som naturlig at en regnskapsfører utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte regnskapsførere. Fullmakt hos regnskapfører oppstår når kunden registrerer regnskapsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.   " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:ansvarligrevisor", Name = "Ansvarlig revisor", Description = "Denne fullmakten gir revisor tilgang til å opptre som ansvarlig revisor for en kunde og utføre alle tjenester som krever denne fullmakten. Dette er tjenester som tjenestetilbyder har vurdert det som naturlig at en revisor utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte revisorer. Fullmakt hos revisor oppstår når kunden registrerer regnskapsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:revisormedarbeider", Name = "Revisormedarbeider", Description = "Denne fullmakten gir revisor tilgang til å opptre som revisormedarbeider for en kunde og utføre alle tjenester som krever denne fullmakten. Dette er tjenester som tjenestetilbyder har vurdert det som naturlig at en revisor utfører på vegne av sin kunde. Fullmakten gis kun til autoriserte revisorer. Fullmakt hos revisor oppstår når kunden registrerer regnskapsfører i Enhetsregisteret. Ved regelverksendringer eller innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.   " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:konkursbotilgangsstyring", Name = "Konkursbo tilgangsstyring", Description = "Denne fullmakten gir rettighet til å administrere konkursbo. Fullmakten er en engangsdelegering, og den gir ikke tilgang til noen tjenester.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:konkursbolesetilgang", Name = "Konkursbo lesetilgang", Description = "Denne fullmakten delegeres til kreditorer og andre som skal ha lesetilgang til det enkelte konkursbo.  " });
        packages.Add(new Package() { Id = Guid.NewGuid(), Urn = "urn:altinn:accesspackage:konkursboskrivetilgang", Name = "Konkursbo skrivetilgang", Description = "Denne fullmakten gir bostyrers medhjelper tilgang til å jobbe på vegne av bostyrer. Bostyrer delegerer denne fullmakten sammen med Konkursbo lesetilgang til medhjelper for hvert konkursbo.   " });

        foreach (var item in packages)
        {
            await packageService.Upsert(item);
        }
    }

    /// <summary>
    /// Ingest all
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task<List<IngestResult>> IngestAll(CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        var result = new List<IngestResult>();

        if (config.JsonIngestEnabled.ContainsKey("providerIngestService") && config.JsonIngestEnabled["providerIngestService"])
        {
            // a?.AddEvent(new System.Diagnostics.ActivityEvent("providerIngestService"));
            result.Add(await IngestData<Provider, IProviderRepository>(providerService, cancellationToken));
        }

        if (config.JsonIngestEnabled.ContainsKey("entityTypeIngestService") && config.JsonIngestEnabled["entityTypeIngestService"])
        {
            // a?.AddEvent(new System.Diagnostics.ActivityEvent("entityTypeIngestService"));
            result.Add(await IngestData<EntityType, IEntityTypeRepository>(entityTypeService, cancellationToken));
        }

        if (config.JsonIngestEnabled.ContainsKey("entityVariantIngestService") && config.JsonIngestEnabled["entityVariantIngestService"])
        {
            // a?.AddEvent(new System.Diagnostics.ActivityEvent("entityVariantIngestService"));
            result.Add(await IngestData<EntityVariant, IEntityVariantRepository>(entityVariantService, cancellationToken));
        }

        if (config.JsonIngestEnabled.ContainsKey("roleIngestService") && config.JsonIngestEnabled["roleIngestService"])
        {
            // a?.AddEvent(new System.Diagnostics.ActivityEvent("roleIngestService"));
            result.Add(await IngestData<Role, IRoleRepository>(roleService, cancellationToken));
        }

        if (config.JsonIngestEnabled.ContainsKey("roleMapIngestService") && config.JsonIngestEnabled["roleMapIngestService"])
        {
            // a?.AddEvent(new System.Diagnostics.ActivityEvent("roleMapIngestService"));
            result.Add(await IngestData<RoleMap, IRoleMapRepository>(roleMapService, cancellationToken));
        }

        if (config.JsonIngestEnabled.ContainsKey("areasAndPackagesIngestService") && config.JsonIngestEnabled["areasAndPackagesIngestService"])
        {
            // a?.AddEvent(new System.Diagnostics.ActivityEvent("areasAndPackagesIngestService"));
            result.AddRange(await IngestAreasAndPackages(cancellationToken));
        }

        if (config.JsonIngestEnabled.ContainsKey("RolePackagesIngestService") && config.JsonIngestEnabled["RolePackagesIngestService"])
        {
            // a?.AddEvent(new System.Diagnostics.ActivityEvent("RolePackagesIngestService"));
            result.AddRange(await IngestRolePackages(cancellationToken));
        }

        if (config.JsonIngestEnabled.ContainsKey("entityVariantRoleIngestService") && config.JsonIngestEnabled["entityVariantRoleIngestService"])
        {
            // a?.AddEvent(new System.Diagnostics.ActivityEvent("entityVariantRoleIngestService"));
            result.Add(await IngestData<EntityVariantRole, IEntityVariantRoleRepository>(entityVariantRoleService, cancellationToken));
        }

        try
        {
            await TempRolePackageFix();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        return result;
    }

    private async Task TempRolePackageFix()
    {
        /*
         TODO: Ivar: Flytt dette inn i json data (RolePackages)
         */
        var allareas = await areaService.Get();
        var allpackages = await packageService.Get();
        var allroles = await roleService.Get();

        var regnArea = allareas.First(t => t.Name == "Fullmakter for regnskapsfører");
        var reviArea = allareas.First(t => t.Name == "Fullmakter for revisor");
        var bobeArea = allareas.First(t => t.Name == "Fullmakter for konkursbo");

        var regnRole = allroles.First(t => t.Code.ToLower() == "regn");
        var reviRole = allroles.First(t => t.Code.ToLower() == "revi");
        var bobeRole = allroles.First(t => t.Code.ToLower() == "bobe");

        var regnPackages = allpackages.Where(t => t.AreaId == regnArea.Id);
        var reviPackages = allpackages.Where(t => t.AreaId == reviArea.Id);
        var bobePackages = allpackages.Where(t => t.AreaId == bobeArea.Id);

        var rolePackCache = await rolePackageService.Get();

        await MapPackagesToRole(regnRole, regnPackages);
        await MapPackagesToRole(reviRole, reviPackages);
        await MapPackagesToRole(bobeRole, bobePackages);

        async Task MapPackagesToRole(Role role, IEnumerable<Package> packages)
        {
            foreach (var pck in packages)
            {
                if (rolePackCache.Count(t => t.PackageId == pck.Id && t.RoleId == regnRole.Id) > 0)
                {
                    continue;
                }

                try
                {
                    await rolePackageService.Create(new RolePackage()
                    {
                        Id = Guid.NewGuid(),
                        HasAccess = true,
                        CanDelegate = true,
                        PackageId = pck.Id,
                        RoleId = role.Id,
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to map package to role. {pck.Name} => {role.Name}");
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }

    private async Task<IngestResult> IngestData<T, TService>(TService service, CancellationToken cancellationToken)
        where TService : IDbBasicRepository<T>
    {
        var type = typeof(T);
        var translatedItems = new Dictionary<string, List<T>>();

        foreach (var lang in options.Value.JsonIngestLanguages.Distinct())
        {
            var translatedJsonData = await ReadJsonData<T>(lang, cancellationToken: cancellationToken);
            if (translatedJsonData == "[]")
            {
                continue;
            }

            var translatedJsonItems = JsonSerializer.Deserialize<List<T>>(translatedJsonData, options: new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            if (translatedJsonItems == null)
            {
                // send ut en feil her
                continue;
            }

            translatedItems.Add(lang, translatedJsonItems);
        }

        var jsonData = await ReadJsonData<T>(cancellationToken: cancellationToken);
        if (jsonData == "[]")
        {
            return new IngestResult(type) { Success = false };
        }

        var jsonItems = JsonSerializer.Deserialize<List<T>>(jsonData, options: new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        if (jsonItems == null)
        {
            return new IngestResult(type) { Success = false };
        }

        return await IngestData(service, jsonItems, translatedItems, cancellationToken);
    }

    private async Task<IngestResult> IngestData<T, TService>(TService service, List<T> jsonItems, Dictionary<string, List<T>> languageJsonItems, CancellationToken cancellationToken)
        where TService : IDbBasicRepository<T>
    {
        var dbItems = await service.Get();

        Console.WriteLine($"Ingest {typeof(T).Name} Json:{jsonItems.Count} Db:{dbItems.Count()}");

        if (dbItems == null || !dbItems.Any())
        {
            foreach (var item in jsonItems)
            {
                await service.Create(item);
            }
        }
        else
        {
            foreach (var item in jsonItems)
            {
                if (dbItems.Count(t => IdComparer(item, t)) == 0)
                {
                    await service.Create(item);
                    continue;
                }

                if (dbItems.Count(t => PropertyComparer(item, t)) == 0)
                {
                    var id = GetId(item);
                    if (id.HasValue)
                    {
                        await service.Update(id.Value, item);
                    }
                }
            }
        }

        await IngestTranslation(service, languageJsonItems, cancellationToken);

        return new IngestResult(typeof(T)) { Success = true };
    }

    private async Task<List<IngestResult>> IngestRolePackages(CancellationToken cancellationToken = default, string language = "")
    {
        var result = new List<IngestResult>();
        var jsonData = await ReadJsonData("NewRolePackages", language);
        var metaRolePackages = JsonSerializer.Deserialize<List<MetaRolePackage>>(jsonData, options: new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        var allpackages = await packageService.Get();
        var allroles = await roleService.Get();
        var uniquePackages = new Dictionary<string, Guid>();
        var uniqueRoles = new Dictionary<string, Guid>();
        var rolePackages = new List<RolePackage>();

        foreach (var rolePackage in metaRolePackages)
        {
            try
            {
                uniquePackages.TryAdd(rolePackage.Tilgangspakke, allpackages.First(x => x.Name == rolePackage.Tilgangspakke).Id);
                foreach (var role in rolePackage.Enhetsregisterroller)
                {
                    uniqueRoles.TryAdd(role, allroles.First(x => x.Name.ToLower() == role.ToLower()).Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {rolePackage.Name}");
                Console.WriteLine(ex.Message);
            }
        }

        foreach (var rolePackage in metaRolePackages)
        {
            foreach (var role in rolePackage.Enhetsregisterroller)
            {
                try
                {
                    rolePackages.Add(new RolePackage
                    {
                        Id = Guid.NewGuid(),
                        PackageId = uniquePackages[rolePackage.Tilgangspakke],
                        RoleId = uniqueRoles[role],
                        HasAccess = rolePackage.HarTilgang,
                        CanDelegate = rolePackage.Delegerbar,
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {rolePackage.Tilgangspakke} - {role}");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        var dbItems = await rolePackageService.Get();

        foreach (var item in rolePackages)
        {
            var dbItem = dbItems.FirstOrDefault(x => x.RoleId.ToString() == item.RoleId.ToString() && x.PackageId.ToString() == item.PackageId.ToString());
            item.Id = dbItem != null ? dbItem.Id : Guid.NewGuid();
        }

        result.Add(await IngestData(rolePackageService, rolePackages, new Dictionary<string, List<RolePackage>>(), cancellationToken));

        return result;
    }

    private async Task<List<IngestResult>> IngestAreasAndPackages(CancellationToken cancellationToken = default)
    {
        var ingestResult = new List<IngestResult>();
        var result = await ReadAndSplitAreasAndPackagesJson();
        var resultEng = await ReadAndSplitAreasAndPackagesJson("eng");
        var resultNno = await ReadAndSplitAreasAndPackagesJson("nno");

        var areaGroupItems = new Dictionary<string, List<AreaGroup>>
        {
            { "nno", resultNno.AreaGroupItems },
            { "eng", resultEng.AreaGroupItems }
        };

        var areaItems = new Dictionary<string, List<Area>>
        {
            { "nno", resultNno.AreaItems },
            { "eng", resultEng.AreaItems }
        };

        var packageItems = new Dictionary<string, List<Package>>
        {
            { "nno", resultNno.PackageItems },
            { "eng", resultEng.PackageItems }
        };

        ingestResult.Add(await IngestData(areaGroupService, result.AreaGroupItems, areaGroupItems, cancellationToken));
        ingestResult.Add(await IngestData(areaService, result.AreaItems, areaItems, cancellationToken));
        ingestResult.Add(await IngestData(packageService, result.PackageItems, packageItems, cancellationToken));
        return ingestResult;
    }

    private async Task<(List<AreaGroup> AreaGroupItems, List<Area> AreaItems, List<Package> PackageItems)> ReadAndSplitAreasAndPackagesJson(string language = "")
    {
        List<AreaGroup> areaGroups = [];
        List<Area> areas = [];
        List<Package> packages = [];

        // TODO: Check if this should be in json data
        var entityType = (await entityTypeService.Get(t => t.Name, "Organisasjon")).First() ?? throw new Exception("Unable to find 'Organisasjon' entityType");

        // TODO: Check if this should be in json data
        var provider = (await providerService.Get(t => t.Name, "Digdir")).First() ?? throw new Exception("Unable to find 'Digdir' provider");

        var jsonData = await ReadJsonData("AreaAndPackages", language);
        List<MetaAreaGroup> metaAreaGroups = [.. JsonSerializer.Deserialize<List<MetaAreaGroup>>(jsonData, options: new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })];

        foreach (var meta in metaAreaGroups)
        {
            areaGroups.Add(new AreaGroup()
            {
                Id = meta.Id,
                Name = meta.Name,
                Description = meta.Description,
                EntityTypeId = (await entityTypeService.Get(t => t.Name, meta.Type)).First().Id,
                Urn = meta.Urn,
            });

            if (meta.Areas != null)
            {
                foreach (var area in meta.Areas)
                {
                    areas.Add(new Area()
                    {
                        Id = area.Id,
                        Name = area.Name,
                        Description = area.Description,
                        GroupId = meta.Id,
                        IconName = area.Icon,
                        Urn = area.Urn,
                    });

                    if (area.Packages != null)
                    {
                        foreach (var package in area.Packages)
                        {
                            packages.Add(new Package()
                            {
                                Id = package.Id,
                                Name = package.Name,
                                Description = package.Description,
                                AreaId = area.Id,
                                EntityTypeId = entityType.Id,
                                ProviderId = provider.Id,
                                IsDelegable = true,
                                HasResources = true,
                                Urn = package.Urn,
                            });
                        }
                    }
                }
            }
        }

        return (areaGroups, areas, packages);
    }

    private async Task<string> ReadJsonData<T>(string? language = null, CancellationToken cancellationToken = default)
    {
        return await ReadJsonData(typeof(T).Name, language, cancellationToken);
    }

    private async Task<string> ReadJsonData(string baseName, string? language = null, CancellationToken cancellationToken = default)
    {
        string fileName = $"{options.Value.JsonBasePath}{Path.DirectorySeparatorChar}{baseName}{(string.IsNullOrEmpty(language) ? string.Empty : "_" + language)}.json";
        if (File.Exists(fileName))
        {
            return await File.ReadAllTextAsync(fileName, cancellationToken);
        }

        return "[]";
    }

    private async Task IngestTranslation<T, TService>(TService service, Dictionary<string, List<T>> languageJsonItems, CancellationToken cancellationToken)
         where TService : IDbBasicRepository<T>
    {
        var type = typeof(T);
        foreach (var translatedItems in languageJsonItems)
        {
            var dbItems = await service.Get(options: new RequestOptions() { Language = translatedItems.Key });
            //// Console.WriteLine($"Ingest {lang} {type.Name} Json:{jsonItems.Count} Db:{dbItems.Count()}");
            foreach (var item in translatedItems.Value)
            {
                try
                {
                    var id = GetId(item);
                    if (id == null)
                    {
                        throw new Exception($"Failed to get Id for '{typeof(T).Name}'");
                    }

                    var rowchanges = await service.UpdateTranslation(id.Value, item, translatedItems.Key);
                    if (rowchanges > 0)
                    {
                        continue;
                    }
                }
                catch
                {
                    Console.WriteLine("Failed to update translation");
                }

                try
                {
                    await service.CreateTranslation(item, translatedItems.Key);
                }
                catch
                {
                    Console.WriteLine("Failed to create translation");
                }
            }
        }
    }

    private TValue? GetValue<T, TValue>(T item, string propertyName)
    {
        var pt = typeof(T).GetProperty(propertyName);
        if (pt == null)
        {
            return default;
        }

        return (TValue?)pt.GetValue(item) ?? default;
    }

    private Guid? GetId<T>(T item)
    {
        return GetValue<T, Guid?>(item, "Id");
    }

    private bool PropertyComparer<T>(T a, T b)
    {
        foreach (var prop in typeof(T).GetProperties())
        {
            if (prop.PropertyType.Name.ToLower() == "string")
            {
                string? valueA = (string?)prop.GetValue(a);
                string? valueB = (string?)prop.GetValue(b);
                if (string.IsNullOrEmpty(valueA) || string.IsNullOrEmpty(valueB))
                {
                    return false;
                }

                if (!valueA.Equals(valueB))
                {
                    return false;
                }
            }

            if (prop.PropertyType.Name.ToLower() == "guid")
            {
                Guid? valueA = (Guid?)prop.GetValue(a);
                Guid? valueB = (Guid?)prop.GetValue(b);
                if (!valueA.HasValue || !valueB.HasValue)
                {
                    return false;
                }

                if (!valueA.Equals(valueB))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IdComparer<T>(T a, T b)
    {
        try
        {
            var idA = GetId(a);
            var idB = GetId(b);
            if (!idA.HasValue || !idB.HasValue)
            {
                return false;
            }

            if (idA.Value.Equals(idB.Value))
            {
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
