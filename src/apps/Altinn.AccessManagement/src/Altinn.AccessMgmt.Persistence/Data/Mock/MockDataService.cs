using System;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.Persistence.Data.Mock;

/// <summary>
/// Mockdata
/// </summary>
public class MockDataService
    (
    IEntityTypeRepository entityTypeRepository,
    IEntityVariantRepository entityVariantRepository,
    IEntityRepository entityRepository,
    IRoleRepository roleRepository,
    IAssignmentRepository assignmentRepository,
    IDelegationRepository delegationRepository,
    IPackageRepository packageRepository,
    IPackageResourceRepository packageResourceRepository,
    IResourceRepository resourceRepository,
    IResourceTypeRepository resourceTypeRepository,
    IProviderRepository providerRepository,
    IDelegationPackageRepository delegationPackageRepository,
    IAssignmentPackageRepository assignmentPackageRepository,
    IConnectionPackageRepository connectionPackageRepository
    )
{
    private readonly IEntityTypeRepository entityTypeRepository = entityTypeRepository;
    private readonly IEntityVariantRepository entityVariantRepository = entityVariantRepository;
    private readonly IEntityRepository entityRepository = entityRepository;
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IAssignmentRepository assignmentRepository = assignmentRepository;
    private readonly IDelegationRepository delegationRepository = delegationRepository;
    private readonly IPackageRepository packageRepository = packageRepository;
    private readonly IPackageResourceRepository packageResourceRepository = packageResourceRepository;
    private readonly IResourceRepository resourceRepository = resourceRepository;
    private readonly IResourceTypeRepository resourceTypeRepository = resourceTypeRepository;
    private readonly IProviderRepository providerRepository = providerRepository;
    private readonly IDelegationPackageRepository delegationPackageRepository = delegationPackageRepository;
    private readonly IAssignmentPackageRepository assignmentPackageRepository = assignmentPackageRepository;
    private readonly IConnectionPackageRepository connectionPackageRepository = connectionPackageRepository;


    public async Task SystemUserClientDelegation()
    {
        var options = new ChangeRequestOptions()
        {
            ChangedBy = AuditDefaults.StaticDataIngest,
            ChangedBySystem = AuditDefaults.StaticDataIngest
        };

        var orgType = (await entityTypeRepository.Get(t => t.Name, "Organisasjon")).FirstOrDefault() ?? throw new Exception("Could not find type 'Organisasjon'");
        var persType = (await entityTypeRepository.Get(t => t.Name, "Person")).FirstOrDefault() ?? throw new Exception("Could not find type 'Person'");
        var sysType = (await entityTypeRepository.Get(t => t.Name, "Systembruker")).FirstOrDefault() ?? throw new Exception("Could not find type 'Systembruker'");
        var variants = await entityVariantRepository.Get();
        var variantAS = variants.First(t => t.TypeId == orgType.Id && t.Name == "AS");
        var variantPers = variants.First(t => t.TypeId == persType.Id && t.Name == "Person");
        var variantSys = variants.First(t => t.TypeId == sysType.Id);

        var roles = await roleRepository.Get();
        var roleDagligLeder = roles.FirstOrDefault(t => t.Code == "daglig-leder");
        var roleStyreLeder = roles.FirstOrDefault(t => t.Code == "styreleder");
        var roleStyreMedlem = roles.FirstOrDefault(t => t.Code == "styremedlem");
        var roleRevisor = roles.FirstOrDefault(t => t.Code == "revisor");
        var roleRegnskap = roles.FirstOrDefault(t => t.Code == "regnskapsforer");
        var roleAgent = roles.FirstOrDefault(t => t.Code == "agent");

        var greenIT = new Entity() { Id = Guid.Parse("0195efb8-7c80-783b-a21b-6dc365681ff2"), Name = "Green IT AS", RefId = "9-ORG-001", TypeId = orgType.Id, VariantId = variantAS.Id };
        var blueAccounts = new Entity() { Id = Guid.Parse("0195efb8-7c80-721c-b4a7-bc140a51baca"), Name = "Blue Accounts AS", RefId = "9-ORG-002", TypeId = orgType.Id, VariantId = variantAS.Id };
        var orangeSoftware = new Entity() { Id = Guid.Parse("0195efb8-7c80-7557-8f20-06c8b65759e1"), Name = "Orange Software", RefId = "9-ORG-003", TypeId = orgType.Id, VariantId = variantAS.Id };
        var systemUserA = new Entity() { Id = Guid.Parse("0195efb8-7c80-7808-8c84-c535f4a3b88f"), Name = "OrangeSystemUser01", RefId = "9-SYS-01", TypeId = sysType.Id, VariantId = variantSys.Id };

        await entityRepository.Upsert(greenIT, options);
        await entityRepository.Upsert(blueAccounts, options);
        await entityRepository.Upsert(orangeSoftware, options);
        await entityRepository.Upsert(systemUserA, options);

        var ass01 = new Assignment() { Id = Guid.Parse("0195efb8-7c80-77ef-8fd9-9b6c05d22ab8"), FromId = greenIT.Id, ToId = blueAccounts.Id, RoleId = roleRegnskap.Id };
        var ass02 = new Assignment() { Id = Guid.Parse("0195efb8-7c80-7791-b9ef-fdda5d915a41"), FromId = blueAccounts.Id, ToId = systemUserA.Id, RoleId = roleAgent.Id };

        await assignmentRepository.Upsert(ass01, options: options);
        await assignmentRepository.Upsert(ass02, options: options);

        var delegation01 = new Delegation() { Id = Guid.Parse("0195efb8-7c80-7d6e-abd0-acefd1e2081e"), FromId = ass01.Id, ToId = ass02.Id, FacilitatorId = blueAccounts.Id };

        await delegationRepository.Upsert(delegation01, options: options);
    }

    /// <summary>
    /// Create basic mockdata
    /// </summary>
    /// <returns></returns>
    public async Task GenerateBasicData()
    {
        var options = new ChangeRequestOptions()
        {
            ChangedBy = AuditDefaults.StaticDataIngest,
            ChangedBySystem = AuditDefaults.StaticDataIngest
        };

        var orgType = (await entityTypeRepository.Get(t => t.Name, "Organisasjon")).FirstOrDefault() ?? throw new Exception("Could not find type 'Organisasjon'");
        var persType = (await entityTypeRepository.Get(t => t.Name, "Person")).FirstOrDefault() ?? throw new Exception("Could not find type 'Person'");
        var variants = await entityVariantRepository.Get();
        var variantAS = variants.First(t => t.TypeId == orgType.Id && t.Name == "AS");
        var variantPers = variants.First(t => t.TypeId == persType.Id && t.Name == "Person");
        var roles = await roleRepository.Get();
        var roleDagligLeder = roles.FirstOrDefault(t => t.Code == "daglig-leder");
        var roleStyreLeder = roles.FirstOrDefault(t => t.Code == "styreleder");
        var roleStyreMedlem = roles.FirstOrDefault(t => t.Code == "styremedlem");
        var roleRevisor = roles.FirstOrDefault(t => t.Code == "revisor");
        var roleRegnskap = roles.FirstOrDefault(t => t.Code == "regnskapsforer");
        var roleAgent = roles.FirstOrDefault(t => t.Code == "agent");

        var spirhAS = new Entity() { Id = Guid.Parse("DDC63ADF-6513-4570-8DD0-21D6B7A55001"), Name = "Spirh AS", RefId = "ORG-000", TypeId = orgType.Id, VariantId = variantAS.Id };
        var bakerHansenAS = new Entity() { Id = Guid.Parse("212B4355-CE4D-4672-93BB-073AEC2BFC1E"), Name = "Baker Hansen", RefId = "ORG-001", TypeId = orgType.Id, VariantId = variantAS.Id };
        var regnskapsfolkAS = new Entity() { Id = Guid.Parse("02B0602E-9991-4E2F-9667-10B8F9D0C5A4"), Name = "Regnskapsfolk AS", RefId = "ORG-002", TypeId = orgType.Id, VariantId = variantAS.Id };
        var revisjonstroll = new Entity() { Id = Guid.Parse("2571B708-561B-4A33-92A9-1C53B439DE5B"), Name = "Revisjonstroll", RefId = "ORG-003", TypeId = orgType.Id, VariantId = variantAS.Id };
        var bakerNordbyAS = new Entity() { Id = Guid.Parse("E9191151-25C8-4D4D-807E-1F3C930AEB60"), Name = "Baker Nordby", RefId = "ORG-004", TypeId = orgType.Id, VariantId = variantAS.Id };
        var agderKyllingAS = new Entity() { Id = Guid.Parse("B2432FB4-744C-404B-9298-03FC282D5B4A"), Name = "Agder Kylling AS", RefId = "ORG-005", TypeId = orgType.Id, VariantId = variantAS.Id };
        var norskRegnskap = new Entity() { Id = Guid.Parse("B82839EE-9F16-4398-8C98-ECB7682E8418"), Name = "Norsk Regnskap AS", RefId = "ORG-006", TypeId = orgType.Id, VariantId = variantAS.Id };

        await entityRepository.Upsert(spirhAS, options);
        await entityRepository.Upsert(bakerHansenAS, options);
        await entityRepository.Upsert(regnskapsfolkAS, options);
        await entityRepository.Upsert(revisjonstroll, options);
        await entityRepository.Upsert(bakerNordbyAS, options);
        await entityRepository.Upsert(agderKyllingAS, options);
        await entityRepository.Upsert(norskRegnskap, options);

        var mariusThuen = new Entity() { Id = Guid.Parse("3ECA9413-F58C-4205-8ED4-2322E1C5E5C0"), Name = "Marius Thuen", RefId = "PERS-000", TypeId = persType.Id, VariantId = variantPers.Id };
        var fredrikJohnsen = new Entity() { Id = Guid.Parse("B238C6ED-D186-410D-983F-2B4AA887F376"), Name = "Fredrik Johnsen", RefId = "PERS-001", TypeId = persType.Id, VariantId = variantPers.Id };
        var annaLindeberg = new Entity() { Id = Guid.Parse("A4464915-41F1-4EAA-8B06-2ED8AE00CDD4"), Name = "Anna Lindeberg", RefId = "PERS-005", TypeId = persType.Id, VariantId = variantPers.Id };
        var sivertMoestue = new Entity() { Id = Guid.Parse("4EE4F732-430A-4502-A67D-47139087C7FD"), Name = "Sivert Moestue", RefId = "PERS-002", TypeId = persType.Id, VariantId = variantPers.Id };
        var gunnarHansen = new Entity() { Id = Guid.Parse("26C57500-9F6D-488A-A49C-512307D130FD"), Name = "Gunnar Hansen", RefId = "PERS-003", TypeId = persType.Id, VariantId = variantPers.Id };
        var kjetilNordby = new Entity() { Id = Guid.Parse("2C2D1E7E-5C67-4E0A-A368-537C074CE484"), Name = "Kjetil Nordby", RefId = "PERS-004", TypeId = persType.Id, VariantId = variantPers.Id };
        var nicolineWaltersen = new Entity() { Id = Guid.Parse("36AFE80D-7FB2-4053-9E6C-5BDE5F7D7084"), Name = "Nicoline Waltersen", RefId = "PERS-006", TypeId = persType.Id, VariantId = variantPers.Id };
        var viggoPettersen = new Entity() { Id = Guid.Parse("5E4AB4D0-2C02-491E-9B26-710F7AFABB5A"), Name = "Viggo Pettersen", RefId = "PERS-007", TypeId = persType.Id, VariantId = variantPers.Id };
        var petterStromstad = new Entity() { Id = Guid.Parse("68F9DB99-4CA7-4388-B572-74DE908C2A95"), Name = "Petter Strømstad", RefId = "PERS-008", TypeId = persType.Id, VariantId = variantPers.Id };
        var oleJohnnyMartinsen = new Entity() { Id = Guid.Parse("8F005D82-482B-4B05-8D84-78CBADB125BA"), Name = "Ole-Johnny Martinsen", RefId = "PERS-009", TypeId = persType.Id, VariantId = variantPers.Id };
        var carlOveJensen = new Entity() { Id = Guid.Parse("829703B5-D9A0-4E89-AAFB-672BBE6DFC01"), Name = "Carl Ove Jensen", RefId = "PERS-010", TypeId = persType.Id, VariantId = variantPers.Id };
        var martinGrundt = new Entity() { Id = Guid.Parse("01B55CE6-E206-4443-B56C-762698F62238"), Name = "Martin Grundt", RefId = "PERS-011", TypeId = persType.Id, VariantId = variantPers.Id };
        var edithTommesen = new Entity() { Id = Guid.Parse("BDA6328F-CEA3-4DFD-A53A-8D5225F94A7E"), Name = "Edith Tommesen", RefId = "PERS-012", TypeId = persType.Id, VariantId = variantPers.Id };

        await entityRepository.Upsert(mariusThuen, options);
        await entityRepository.Upsert(fredrikJohnsen, options);
        await entityRepository.Upsert(annaLindeberg, options);
        await entityRepository.Upsert(sivertMoestue, options);
        await entityRepository.Upsert(gunnarHansen, options);
        await entityRepository.Upsert(kjetilNordby, options);
        await entityRepository.Upsert(nicolineWaltersen, options);
        await entityRepository.Upsert(viggoPettersen, options);
        await entityRepository.Upsert(petterStromstad, options);
        await entityRepository.Upsert(oleJohnnyMartinsen, options);
        await entityRepository.Upsert(carlOveJensen, options);
        await entityRepository.Upsert(martinGrundt, options);
        await entityRepository.Upsert(edithTommesen, options);

        await assignmentRepository.Upsert(new Assignment() { FromId = spirhAS.Id, ToId = mariusThuen.Id, RoleId = roleDagligLeder.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = spirhAS.Id, ToId = mariusThuen.Id, RoleId = roleStyreLeder.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = bakerHansenAS.Id, ToId = fredrikJohnsen.Id, RoleId = roleDagligLeder.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = bakerHansenAS.Id, ToId = annaLindeberg.Id, RoleId = roleStyreLeder.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = bakerHansenAS.Id, ToId = gunnarHansen.Id, RoleId = roleStyreMedlem.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = regnskapsfolkAS.Id, ToId = sivertMoestue.Id, RoleId = roleDagligLeder.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = regnskapsfolkAS.Id, ToId = nicolineWaltersen.Id, RoleId = roleStyreLeder.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = revisjonstroll.Id, ToId = viggoPettersen.Id, RoleId = roleDagligLeder.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = revisjonstroll.Id, ToId = petterStromstad.Id, RoleId = roleStyreLeder.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = bakerNordbyAS.Id, ToId = kjetilNordby.Id, RoleId = roleDagligLeder.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = bakerNordbyAS.Id, ToId = kjetilNordby.Id, RoleId = roleStyreLeder.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = spirhAS.Id, ToId = norskRegnskap.Id, RoleId = roleRegnskap.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = bakerHansenAS.Id, ToId = regnskapsfolkAS.Id, RoleId = roleRegnskap.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = bakerHansenAS.Id, ToId = revisjonstroll.Id, RoleId = roleRevisor.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = bakerNordbyAS.Id, ToId = regnskapsfolkAS.Id, RoleId = roleRegnskap.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(new Assignment() { FromId = regnskapsfolkAS.Id, ToId = revisjonstroll.Id, RoleId = roleRevisor.Id }, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);

        var assignment001 = new Assignment() { FromId = agderKyllingAS.Id, ToId = carlOveJensen.Id, RoleId = roleDagligLeder.Id };
        var assignment002 = new Assignment() { FromId = agderKyllingAS.Id, ToId = norskRegnskap.Id, RoleId = roleRegnskap.Id };
        var assignment003 = new Assignment() { FromId = norskRegnskap.Id, ToId = martinGrundt.Id, RoleId = roleDagligLeder.Id };
        var assignment004 = new Assignment() { FromId = norskRegnskap.Id, ToId = edithTommesen.Id, RoleId = roleAgent.Id };
        await assignmentRepository.Upsert(assignment001, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(assignment002, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(assignment003, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);
        await assignmentRepository.Upsert(assignment004, updateProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], compareProperties: [t => t.FromId, t => t.ToId, t => t.RoleId], options: options);

    }

    private async Task GeneratePackageResources(ChangeRequestOptions options)
    {
        var packages = await packageRepository.GetExtended();
        var resourceTypes = await resourceTypeRepository.Get();
        if (resourceTypes == null || !resourceTypes.Any())
        {
            await resourceTypeRepository.Create(new ResourceType() { Name = "Default" }, options);
            resourceTypes = await resourceTypeRepository.Get();
        }

        var provider = (await providerRepository.Get(t => t.Name, "Digitaliseringsdirektoratet")).FirstOrDefault() ?? throw new Exception("Provider not found");

        foreach (var package in packages)
        {
            var resources = await packageResourceRepository.GetB(package.Id);
            if (resources == null || !resources.Any())
            {
                for (int i = 0; i < 5; i++)
                {
                    string title = GetRandomResourceTitle(i);
                    var resourceId = Guid.NewGuid();
                    await resourceRepository.Create(
                        new Resource()
                        {
                            Name = GetRandomResourceTitle(i),
                            Description = "Somthing generated for the " + package.Name,
                            RefId = resourceId.ToString().ToLower(),
                            TypeId = resourceTypes.OrderBy(t => Guid.NewGuid()).First().Id,
                            ProviderId = provider.Id
                        }, 
                        options: options
                    );

                    await packageResourceRepository.Create(
                        new PackageResource()
                        {
                            PackageId = package.Id,
                            ResourceId = resourceId
                        },
                        options: options
                    );
                }
            }
        }
    }

    private static readonly Random _random = new Random();

    private static string GetRandomResourceTitle(int no)
    {
        string[] values1 = { "Skjema", "Rapport", "Søknad" };
        string[] values2 = { "No.00", "#", "TSQ-", "000001" };

        int index1 = _random.Next(values1.Length);
        int index2 = _random.Next(values2.Length);
        return $"{values1[index1]} {values2[index2]}{no}";
    }
}
