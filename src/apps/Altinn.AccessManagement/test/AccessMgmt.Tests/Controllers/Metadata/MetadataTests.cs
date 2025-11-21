using AccessMgmt.Tests.Services;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace AccessMgmt.Tests.Controllers.Metadata;

public class MetadataTests : IClassFixture<PostgresFixture>
{
    private readonly AppDbContext _db;

    public MetadataTests(PostgresFixture fixture)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.SharedDb.Admin.ToString())
            .Options;

        _db = new AppDbContext(options);

        SeedTestData(_db).Wait();
    }

    public List<Resource> Resources { get; set; } = new List<Resource>();

    private async Task SeedTestData(AppDbContext db)
    {
        var resourceType = new ResourceType() { Id = Guid.NewGuid(), Name = "Test" };
        db.ResourceTypes.Add(resourceType);

        Resources = new List<Resource>()
        {
            new Resource() { Id = Guid.CreateVersion7(), Name = "Test #01", Description = "Description of Test#01", RefId = "T-01", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.CreateVersion7(), Name = "Test #02", Description = "Description of Test#02", RefId = "T-02", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.CreateVersion7(), Name = "Test #03", Description = "Description of Test#03", RefId = "T-03", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.CreateVersion7(), Name = "Test #04", Description = "Description of Test#04", RefId = "T-04", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.CreateVersion7(), Name = "Test #05", Description = "Description of Test#05", RefId = "T-05", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.CreateVersion7(), Name = "Test #06", Description = "Description of Test#06", RefId = "T-06", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
            new Resource() { Id = Guid.CreateVersion7(), Name = "Test #07", Description = "Description of Test#07", RefId = "T-07", TypeId = resourceType.Id, ProviderId = ProviderConstants.Altinn3.Id },
        };

        foreach (var resource in Resources)
        {
            db.Resources.Add(resource);
        }

        db.RoleResources.Add(new RoleResource() { RoleId = RoleConstants.ManagingDirector.Id, ResourceId = Resources.First(t => t.RefId == "T-01").Id });
        db.RoleResources.Add(new RoleResource() { RoleId = RoleConstants.ManagingDirector.Id, ResourceId = Resources.First(t => t.RefId == "T-02").Id });
        db.RoleResources.Add(new RoleResource() { RoleId = RoleConstants.Accountant.Id, ResourceId = Resources.First(t => t.RefId == "T-03").Id });
        db.RoleResources.Add(new RoleResource() { RoleId = RoleConstants.MainAdministrator.Id, ResourceId = Resources.First(t => t.RefId == "T-04").Id });

        db.PackageResources.Add(new PackageResource() { PackageId = PackageConstants.Catering.Id, ResourceId = Resources.First(t => t.RefId == "T-05").Id });
        db.PackageResources.Add(new PackageResource() { PackageId = PackageConstants.Catering.Id, ResourceId = Resources.First(t => t.RefId == "T-06").Id });


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

    #region Role Packages
    [Fact]
    public async Task DagligLeder_Code_AS_Shold_Have_Packages()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db));

        var res = await controller.GetPackages(RoleConstants.ManagingDirector.Entity.Code, "AS", includeResources: false);
        Assert.NotNull(res.Value);
    }

    [Fact]
    public async Task DagligLeder_Code_AS_Shold_Have_PackageResources()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db));

        var res = await controller.GetPackages(RoleConstants.ManagingDirector.Entity.Code, "AS", includeResources: true);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.SelectMany(t => t.Resources).Count() > 0);
    }

    [Fact]
    public async Task DagligLeder_Id_AS_Shold_Have_Packages()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db));

        var res = await controller.GetPackages(RoleConstants.ManagingDirector.Id, "AS", includeResources: false);
        Assert.NotNull(res.Value);
    }

    [Fact]
    public async Task DagligLeder_Id_AS_Shold_Have_PackageResources()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db));

        var res = await controller.GetPackages(RoleConstants.ManagingDirector.Id, "AS", includeResources: true);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.SelectMany(t => t.Resources).Count() > 0);
    }
    #endregion

    #region Role Resources
    [Fact]
    public async Task DagligLeder_Code_AS_Shold_Have_Resources()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db));

        var res = await controller.GetResources(RoleConstants.ManagingDirector.Entity.Code, "AS", includePackageResources: false);
        Assert.NotNull(res.Value);
    }

    [Fact]
    public async Task DagligLeder_Code_AS_Shold_Have_Resources_FromPackages()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db));

        var res = await controller.GetResources(RoleConstants.ManagingDirector.Entity.Code, "AS", includePackageResources: true);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.Count(t => t.RefId == "T-05") > 0);
    }

    [Fact]
    public async Task DagligLeder_Id_AS_Shold_Have_Resources()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db));

        var res = await controller.GetResources(RoleConstants.ManagingDirector.Id, "AS", includePackageResources: false);
        Assert.NotNull(res.Value);
        foreach (var resource in res.Value)
        {
            Assert.NotNull(resource.Provider);
        }
    }

    [Fact]
    public async Task DagligLeder_Id_AS_Shold_Have_Resources_FromPackages()
    {
        var controller = new Altinn.AccessManagement.Api.Metadata.Controllers.RolesController(new RoleService(_db));

        var res = await controller.GetResources(RoleConstants.ManagingDirector.Id, "AS", includePackageResources: true);
        Assert.NotNull(res.Value);
        Assert.True(res.Value.Count(t => t.RefId == "T-05") > 0);
        foreach (var resource in res.Value)
        {
            Assert.NotNull(resource.Provider);
        }
    }
    #endregion
}
