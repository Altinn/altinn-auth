using Altinn.AccessManagement.Api.Metadata.Controllers;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AccessMgmt.Tests.Controllers.Metadata;

public class MetadataTests : IClassFixture<PostgresFixture>
{
    private readonly AppDbContext _db;
    private readonly ITranslationService _translationService;

    public MetadataTests(PostgresFixture fixture)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.SharedDb.Admin.ToString())
            .Options;

        _db = new AppDbContext(options);

        // Create a real translation service for tests
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _translationService = new TranslationService(_db, memoryCache);

        SeedTestData(_db).Wait();
    }

    public List<Resource> Resources { get; set; } = new List<Resource>();

    private async Task SeedTestData(AppDbContext db)
    {
        var resourceType = await db.ResourceTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == Guid.Parse("0195efb8-7c80-7f26-817a-50893176320d"));
        if (resourceType is null)
        {
            resourceType = new ResourceType() { Id = Guid.Parse("0195efb8-7c80-7f26-817a-50893176320d"), Name = "Test" };
            db.ResourceTypes.Add(resourceType);
        }

        Resources = new List<Resource>()
        {
            new Resource() { Id = Guid.Parse("0195efb8-7c80-77b5-9b9f-b50590d55d13"), Name = "Test #01", Description = "Description of Test#01", RefId = "T-01", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.Parse("0195efb8-7c80-7b06-8a43-3e86ca9fbf1e"), Name = "Test #02", Description = "Description of Test#02", RefId = "T-02", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.Parse("0195efb8-7c80-72bc-9668-c2e13201de45"), Name = "Test #03", Description = "Description of Test#03", RefId = "T-03", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.Parse("0195efb8-7c80-7397-99bf-91c6bd831353"), Name = "Test #04", Description = "Description of Test#04", RefId = "T-04", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.Parse("0195efb8-7c80-7e57-8228-9a89e982c891"), Name = "Test #05", Description = "Description of Test#05", RefId = "T-05", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.Parse("0195efb8-7c80-76e3-9d02-8761b7ae0ecc"), Name = "Test #06", Description = "Description of Test#06", RefId = "T-06", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.Parse("0195efb8-7c80-75fc-8ab2-7b84e78df7b5"), Name = "Test #07", Description = "Description of Test#07", RefId = "T-07", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
        };

        foreach (var resource in Resources)
        {
            if (db.Resources.AsNoTracking().Count(t => t.Id == resource.Id) == 0)
            {
                db.Resources.Add(resource);
            }
        }

        var roleResources = new List<RoleResource>()
        {
            new RoleResource() { Id = Guid.Parse("0195efb8-7c80-7e5d-afd8-61915a156dcc"), RoleId = RoleConstants.ManagingDirector.Id, ResourceId = Resources.First(t => t.RefId == "T-01").Id },
            new RoleResource() { Id = Guid.Parse("0195efb8-7c80-7988-9880-855732872555"), RoleId = RoleConstants.ManagingDirector.Id, ResourceId = Resources.First(t => t.RefId == "T-02").Id },
            new RoleResource() { Id = Guid.Parse("0195efb8-7c80-7a2d-b5c1-0d355ac62b18"), RoleId = RoleConstants.Accountant.Id, ResourceId = Resources.First(t => t.RefId == "T-03").Id },
            new RoleResource() { Id = Guid.Parse("0195efb8-7c80-710f-bed6-35c99fbc46b1"), RoleId = RoleConstants.MainAdministrator.Id, ResourceId = Resources.First(t => t.RefId == "T-04").Id },
        };

        foreach (var roleResource in roleResources)
        {
            if (db.RoleResources.AsNoTracking().Count(t => t.Id == roleResource.Id) == 0)
            {
                db.RoleResources.Add(roleResource);
            }
        }

        var packageResources = new List<PackageResource>()
        {
            new PackageResource() { PackageId = PackageConstants.Catering.Id, ResourceId = Resources.First(t => t.RefId == "T-05").Id },
            new PackageResource() { PackageId = PackageConstants.Catering.Id, ResourceId = Resources.First(t => t.RefId == "T-06").Id },
        };

        foreach (var packageResource in packageResources)
        {
            if (db.PackageResources.AsNoTracking().Count(t => t.Id == packageResource.Id) == 0)
            {
                db.PackageResources.Add(packageResource);
            }
        }

        if (!db.RolePackages.AsNoTracking().Any(t => t.RoleId == RoleConstants.ManagingDirector.Id && t.PackageId == PackageConstants.Catering.Id))
        {
            db.RolePackages.Add(new RolePackage() { RoleId = RoleConstants.ManagingDirector.Id, PackageId = PackageConstants.Catering.Id });
        }

        try
        {
            await db.SaveChangesAsync(new Altinn.AccessMgmt.PersistenceEF.Extensions.AuditValues(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest));
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.ToString());
        }
    }

    #region Positive Variant Role Package

    /// <summary>
    /// Forretningsfører for ESEK skal ha Forretningsforer eiendom pakken
    /// </summary>
    [Fact]
    public async Task RoleVariantPackage_BusinessManager_ESEK_BusinessManagerRealEstate_Have()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db), _translationService);

        // Forretningsfører for ESEK skal ha Forretningsforer eiendom pakken
        var role = RoleConstants.BusinessManager;
        var variant = EntityVariantConstants.ESEK;
        var package = PackageConstants.BusinessManagerRealEstate;

        await AssertVariantRolePackage(role.Entity, variant.Entity, package.Entity, true);
    }

    [Fact]
    public async Task RoleVariantPackage_BusinessManager_BRL_BusinessManagerRealEstate_Have()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db), _translationService);

        // Forretningsfører for BRL skal ha Forretningsforer eiendom pakken
        var role = RoleConstants.BusinessManager;
        var variant = EntityVariantConstants.BRL;
        var package = PackageConstants.BusinessManagerRealEstate;

        await AssertVariantRolePackage(role.Entity, variant.Entity, package.Entity, true);
    }

    [Fact]
    public async Task RoleVariantPackage_ContactPersonNUF_NUF_DelegableMaskinportenScopesNUF_Have()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db), _translationService);

        // Forretningsfører for BRL skal ha Forretningsforer eiendom pakken
        var role = RoleConstants.ContactPersonNUF;
        var variant = EntityVariantConstants.NUF;
        var package = PackageConstants.DelegableMaskinportenScopesNUF;

        await AssertVariantRolePackage(role.Entity, variant.Entity, package.Entity, true);
    }

    [Fact]
    public async Task RoleVariantPackage_MainAdministrator_NUF_DelegableMaskinportenScopesNUF_Have()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db), _translationService);

        // Forretningsfører for BRL skal ha Forretningsforer eiendom pakken
        var role = RoleConstants.MainAdministrator;
        var variant = EntityVariantConstants.NUF;
        var package = PackageConstants.DelegableMaskinportenScopesNUF;

        await AssertVariantRolePackage(role.Entity, variant.Entity, package.Entity, true);
    }

    [Fact]
    public async Task RoleVariantPackage_Accountant_AS_AccountingPackages_Have()
    {
        /*
        Regnskapsfører med signeringsrettighet => AuditorEmployee
        Regnskapsfører lønn => CentralCoordinationRegister
        Regnskapsfører uten signeringsrettighet => PopulationRegistry
        */

        var role = RoleConstants.Accountant;
        var variant = EntityVariantConstants.AS;

        await AssertVariantRolePackage(role.Entity, variant.Entity, PackageConstants.AuditorEmployee.Entity, true);
        await AssertVariantRolePackage(role.Entity, variant.Entity, PackageConstants.CentralCoordinationRegister.Entity, true);
        await AssertVariantRolePackage(role.Entity, variant.Entity, PackageConstants.PopulationRegistry.Entity, true);
    }

    [Fact]
    public async Task RoleVariantPackage_Auditor_AS_AuditorPackages_Have()
    {
        /*
        Revisormedarbeider => EnforcementOfficer
        Ansvarlig revisor => PersonalIdentityRegistry
        */

        var role = RoleConstants.Auditor;
        var variant = EntityVariantConstants.AS;

        await AssertVariantRolePackage(role.Entity, variant.Entity, PackageConstants.EnforcementOfficer.Entity, true);
        await AssertVariantRolePackage(role.Entity, variant.Entity, PackageConstants.PersonalIdentityRegistry.Entity, true);
    }

    #endregion

    [Fact]
    public async Task RoleVariantPackage_ManagingDirector_NUF_DelegableMaskinportenScopesNUF_Not_Have()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db), _translationService);
        var role = RoleConstants.ManagingDirector;
        var variant = EntityVariantConstants.NUF;
        var package = PackageConstants.DelegableMaskinportenScopesNUF;

        await AssertVariantRolePackage(role.Entity, variant.Entity, package.Entity, false);
    }

    [Fact]
    public async Task RoleVariantPackage_ManagingDirector_ESEK_BusinessManagerRealEstate_Not_Have()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db), _translationService);
        var role = RoleConstants.ManagingDirector;
        var variant = EntityVariantConstants.ESEK;
        var package = PackageConstants.BusinessManagerRealEstate;

        await AssertVariantRolePackage(role.Entity, variant.Entity, package.Entity, false);
    }

    [Fact]
    public async Task RoleVariantPackage_ManagingDirector_BRL_BusinessManagerRealEstate_Not_Have()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db), _translationService);
        var role = RoleConstants.ManagingDirector;
        var variant = EntityVariantConstants.BRL;
        var package = PackageConstants.BusinessManagerRealEstate;

        await AssertVariantRolePackage(role.Entity, variant.Entity, package.Entity, false);
    }

    [Fact]
    public async Task RoleVariantPackage_ManagingDirector_AS_BusinessManagerRealEstate_Not_Have()
    {
        var role = RoleConstants.ManagingDirector;
        var variant = EntityVariantConstants.AS;
        var package = PackageConstants.BusinessManagerRealEstate;

        await AssertVariantRolePackage(role.Entity, variant.Entity, package.Entity, false);
    }

    [Fact]
    public async Task RoleVariantPackage_ManagingDirector_AS_ExplicitServiceDelegation_Not_Have()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db), _translationService);
        var role = RoleConstants.ManagingDirector;
        var variant = EntityVariantConstants.AS;
        var package = PackageConstants.ExplicitServiceDelegation;

        await AssertVariantRolePackage(role.Entity, variant.Entity, package.Entity, false);
    }

    [Fact]
    public async Task RoleVariantPackage_ManagingDirector_AS_ConfidentialMailToBusiness_Not_Have()
    {
        var role = RoleConstants.ManagingDirector;
        var variant = EntityVariantConstants.AS;
        var package = PackageConstants.ConfidentialMailToBusiness;

        await AssertVariantRolePackage(role.Entity, variant.Entity, package.Entity, false);
    }

    [Fact]
    public async Task RoleVariantPackage_ManagingDirector_AS_AccountingPackages_Not_Have()
    {
        /*
        Regnskapsfører med signeringsrettighet => AuditorEmployee
        Regnskapsfører lønn => CentralCoordinationRegister
        Regnskapsfører uten signeringsrettighet => PopulationRegistry
        */

        var role = RoleConstants.ManagingDirector;
        var variant = EntityVariantConstants.AS;

        await AssertVariantRolePackage(role.Entity, variant.Entity, PackageConstants.AuditorEmployee.Entity, false);
        await AssertVariantRolePackage(role.Entity, variant.Entity, PackageConstants.CentralCoordinationRegister.Entity, false);
        await AssertVariantRolePackage(role.Entity, variant.Entity, PackageConstants.PopulationRegistry.Entity, false);
    }

    [Fact]
    public async Task RoleVariantPackage_ManagingDirector_AS_AuditorPackages_Not_Have()
    {
        /*
        Revisormedarbeider => EnforcementOfficer
        Ansvarlig revisor => PersonalIdentityRegistry
        */

        var role = RoleConstants.ManagingDirector;
        var variant = EntityVariantConstants.AS;

        await AssertVariantRolePackage(role.Entity, variant.Entity, PackageConstants.EnforcementOfficer.Entity, false);
        await AssertVariantRolePackage(role.Entity, variant.Entity, PackageConstants.PersonalIdentityRegistry.Entity, false);
    }

    private async Task AssertVariantRolePackage(
    Role role,
    EntityVariant variant,
    Package package,
    bool shouldExist)
    {
        var controller = new RolesController(new RoleService(_db), _translationService);
        var result = await controller.GetPackages(role.Id, variant.Name, includeResources: false);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IEnumerable<PackageDto>>(ok.Value);

        var ids = value.Select(t => t.Id).ToList();

        if (shouldExist)
        {
            Assert.Contains(package.Id, ids);
        }
        else
        {
            Assert.DoesNotContain(package.Id, ids);
        }
    }

    #region Role Packages
    [Fact]
    public async Task DagligLeder_Code_AS_Should_Have_Packages()
    {
        var controller = new RolesController(new RoleService(_db), _translationService);

        var result = await controller.GetPackages(RoleConstants.ManagingDirector.Entity.Code, "AS", includeResources: false);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IEnumerable<PackageDto>>(ok.Value);
    }

    [Fact]
    public async Task DagligLeder_Code_AS_Should_Have_PackageResources()
    {
        var controller = new RolesController(new RoleService(_db), _translationService);
        var result = await controller.GetPackages(RoleConstants.ManagingDirector.Entity.Code, "AS", includeResources: true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IEnumerable<PackageDto>>(ok.Value);

        Assert.True(value.SelectMany(t => t.Resources).Count() > 0);
    }

    [Fact]
    public async Task DagligLeder_Id_AS_Should_Have_Packages()
    {
        var controller = new RolesController(new RoleService(_db), _translationService);
        var result = await controller.GetPackages(RoleConstants.ManagingDirector.Id, "AS", includeResources: false);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IEnumerable<PackageDto>>(ok.Value);
    }

    [Fact]
    public async Task DagligLeder_Id_AS_Should_Have_PackageResources()
    {
        var controller = new RolesController(new RoleService(_db), _translationService);

        var result = await controller.GetPackages(RoleConstants.ManagingDirector.Id, "AS", includeResources: true);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IEnumerable<PackageDto>>(ok.Value);

        Assert.True(value.SelectMany(t => t.Resources).Count() > 0);
    }
    #endregion

    #region Role Resources
    [Fact]
    public async Task DagligLeder_Code_AS_Should_Have_Resources()
    {
        var controller = new RolesController(new RoleService(_db), _translationService);

        var result = await controller.GetResources(RoleConstants.ManagingDirector.Entity.Code, "AS");
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IEnumerable<ResourceDto>>(ok.Value);

        Assert.NotEmpty(value);
    }

    [Fact]
    public async Task DagligLeder_Code_AS_Should_Have_Resources_FromPackages()
    {
        var controller = new RolesController(new RoleService(_db), _translationService);
        var result = await controller.GetResources(RoleConstants.ManagingDirector.Entity.Code, "AS", includePackageResources: true);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IEnumerable<ResourceDto>>(ok.Value);
        Assert.True(value.Count(t => t.RefId == "T-05") > 0);
    }

    [Fact]
    public async Task DagligLeder_Id_AS_Should_Have_Resources()
    {
        var controller = new RolesController(new RoleService(_db), _translationService);

        var result = await controller.GetResources(RoleConstants.ManagingDirector.Id, "AS", includePackageResources: false);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IEnumerable<ResourceDto>>(ok.Value);

        Assert.NotEmpty(value);
    }

    [Fact]
    public async Task DagligLeder_Id_AS_Should_Have_Resources_FromPackages()
    {
        var controller = new RolesController(new RoleService(_db), _translationService);
        var result = await controller.GetResources(RoleConstants.ManagingDirector.Id, "AS", includePackageResources: true);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IEnumerable<ResourceDto>>(ok.Value);
        Assert.True(value.Count(t => t.RefId == "T-05") > 0);
    }
    #endregion
}


/*



0195efb8-7c80-772a-a13c-1267e5065189
0195efb8-7c80-7a3a-b1b0-600c335f3fb7
0195efb8-7c80-7e1c-9618-61b264e3a446
0195efb8-7c80-71dd-9278-00522bde1ccb
0195efb8-7c80-7fb7-b88e-b21042593152
0195efb8-7c80-78bd-808a-7d484b6c2088
0195efb8-7c80-7e9a-be8a-998c8bdd4918
0195efb8-7c80-756c-98e8-cd2ac394e392
0195efb8-7c80-7343-8d09-3181fdd9d57c
0195efb8-7c80-7eab-aa28-afb4f95e9888
0195efb8-7c80-741f-8fc8-c7a954a6b490
0195efb8-7c80-7faa-8dea-196408da9baf
0195efb8-7c80-7747-8f1c-f05429a28a51
0195efb8-7c80-7616-98f4-534702cb3642
0195efb8-7c80-7b13-87e8-7d55d3cb2b7d
0195efb8-7c80-7b82-804f-f34c7ea79058
0195efb8-7c80-791b-8762-98cd5ece2013
0195efb8-7c80-763c-915c-820f9ce219c3
0195efb8-7c80-7450-a99c-8cebfe44dc36
0195efb8-7c80-7954-b450-88d35c468e7c
0195efb8-7c80-7cf8-963d-7d64fc7f8f8a
0195efb8-7c80-7822-ba11-b5941b724a4c
0195efb8-7c80-7571-9a12-ea66fff7d4b2
0195efb8-7c80-76c8-8929-2ef1a8189e35
0195efb8-7c80-74fb-9701-8c61fe965d4a
0195efb8-7c80-77e0-888e-d47f99d1c65a
0195efb8-7c80-756a-aaff-6f5605c16c36
0195efb8-7c80-7c68-b236-e35667d5a5bf
0195efb8-7c80-731a-8837-f515c09876ba
0195efb8-7c80-7fd4-a772-88f06000b376
0195efb8-7c80-7fcc-83ed-1ec16a6fca7c
0195efb8-7c80-7797-81ed-75c4609c619d
0195efb8-7c80-7502-aa43-bc62d19a3712
0195efb8-7c80-7603-b49a-c21ec61124be
0195efb8-7c80-73e0-85e0-83c410f65fa7
0195efb8-7c80-7d84-89ec-88241fc2d9bc
0195efb8-7c80-7b1a-9b3e-b381efb59b4c
0195efb8-7c80-7575-ba9b-ac7a14425377
*/
