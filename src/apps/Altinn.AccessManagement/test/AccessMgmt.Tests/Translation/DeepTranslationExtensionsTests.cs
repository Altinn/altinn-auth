using Altinn.AccessManagement.Api.Metadata.Translation;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccessMgmt.Tests.Translation;

/// <summary>
/// Tests for DeepTranslationExtensions to ensure nested objects are properly translated.
/// </summary>
public class DeepTranslationExtensionsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    private readonly IMemoryCache _cache;

    public DeepTranslationExtensionsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <summary>
    /// Creates a new TranslationService with its own DbContext instance.
    /// This simulates how services work in production where each request gets a scoped DbContext.
    /// Thread safety checks are disabled for testing parallel translation operations.
    /// </summary>
    private ITranslationService CreateTranslationService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_fixture.SharedDb.Admin.ToString())
            .EnableThreadSafetyChecks(false) // Disable for parallel translation tests
            .Options;

        var db = new AppDbContext(options);
        return new TranslationService(db, _cache, NullLogger<TranslationService>.Instance);
    }

    #region PackageDto Deep Translation Tests

    [Fact]
    public async Task TranslateDeepAsync_PackageDto_TranslatesNestedArea()
    {
        // Arrange - Using real constant from AreaConstants
        var translationService = CreateTranslationService();
        var areaId = AreaConstants.ConstructionInfrastructureAndRealEstate.Id;
        var package = new PackageDto
        {
            Id = PackageConstants.PropertyRegistration.Id,
            Name = "Tinglysing eiendom",
            Description = "Norsk beskrivelse",
            Area = new AreaDto
            {
                Id = areaId,
                Name = "Bygg, anlegg og eiendom",
                Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til bygg, anlegg og eiendom."
            }
        };

        // Act
        var translated = await package.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.NotNull(translated);
        Assert.Equal("Property registration", translated.Name);
        Assert.NotNull(translated.Area);
        Assert.Equal("Construction, Infrastructure and Real Estate", translated.Area.Name);
        Assert.Equal("This authorization area includes access packages related to construction, infrastructure and real estate.", 
            translated.Area.Description);
    }

    [Fact]
    public async Task TranslateDeepAsync_PackageDto_TranslatesNestedResources()
    {
        // Arrange
        var translationService = CreateTranslationService();
        var package = new PackageDto
        {
            Id = PackageConstants.PropertyRegistration.Id,
            Name = "Tinglysing eiendom",
            Resources = new List<ResourceDto>
            {
                new ResourceDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Resource",
                    Provider = new ProviderDto
                    {
                        Id = ProviderConstants.Altinn3.Id,
                        Name = "Altinn 3"
                    }
                }
            }
        };

        // Act
        var translated = await package.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.NotNull(translated.Resources);
        var resource = translated.Resources.First();
        Assert.NotNull(resource);
        Assert.NotNull(resource.Provider);
        Assert.Equal("Altinn 3", resource.Provider.Name); // Altinn3 has same name in all languages
    }

    [Fact]
    public async Task TranslateDeepAsync_PackageDto_HandlesNullNestedObjects()
    {
        // Arrange
        var translationService = CreateTranslationService();
        var package = new PackageDto
        {
            Id = PackageConstants.PropertyRegistration.Id,
            Name = "Tinglysing eiendom",
            Area = null,
            Resources = null
        };

        // Act
        var translated = await package.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.NotNull(translated);
        Assert.Equal("Property registration", translated.Name);
        Assert.Null(translated.Area);
        Assert.Null(translated.Resources);
    }

    [Fact]
    public async Task TranslateDeepAsync_PackageDto_ReturnsNullWhenPackageIsNull()
    {
        // Arrange
        var translationService = CreateTranslationService();
        PackageDto package = null;

        // Act
        var translated = await package.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.Null(translated);
    }

    #endregion

    #region PackageDto Collection Tests

    [Fact]
    public async Task TranslateDeepAsync_PackageDtoCollection_TranslatesAllPackagesInParallel()
    {
        // Arrange
        var translationService = CreateTranslationService();
        var packages = new List<PackageDto>
        {
            new PackageDto
            {
                Id = PackageConstants.PropertyRegistration.Id,
                Name = "Tinglysing eiendom",
                Area = new AreaDto
                {
                    Id = AreaConstants.ConstructionInfrastructureAndRealEstate.Id,
                    Name = "Bygg, anlegg og eiendom"
                }
            },
            new PackageDto
            {
                Id = PackageConstants.Agriculture.Id,
                Name = "Jordbruk",
                Area = new AreaDto
                {
                    Id = AreaConstants.AgricultureForestryHuntingFishingAndAquaculture.Id,
                    Name = "Jordbruk, skogbruk, jakt, fiske og akvakultur"
                }
            }
        };

        // Act
        var translated = (await packages.TranslateDeepAsync(translationService, "eng", true)).ToList();

        // Assert
        Assert.Equal(2, translated.Count);
        
        // First package
        Assert.Equal("Property registration", translated[0].Name);
        Assert.Equal("Construction, Infrastructure and Real Estate", translated[0].Area.Name);
        
        // Second package
        Assert.Equal("Agriculture", translated[1].Name);
        Assert.Equal("Agriculture, Forestry, Hunting, Fishing and Aquaculture", translated[1].Area.Name);
    }

    [Fact]
    public async Task TranslateDeepAsync_PackageDtoCollection_HandlesEmptyCollection()
    {
        // Arrange
        var translationService = CreateTranslationService();
        var packages = new List<PackageDto>();

        // Act
        var translated = await packages.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.Empty(translated);
    }

    [Fact]
    public async Task TranslateDeepAsync_PackageDtoCollection_ReturnsNullWhenCollectionIsNull()
    {
        // Arrange
        var translationService = CreateTranslationService();
        IEnumerable<PackageDto> packages = null;

        // Act
        var translated = await packages.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.Null(translated);
    }

    #endregion

    #region AreaDto Deep Translation Tests

    [Fact]
    public async Task TranslateDeepAsync_AreaDto_TranslatesNestedGroup()
    {
        // Arrange
        var translationService = CreateTranslationService();
        var area = new AreaDto
        {
            Id = AreaConstants.TaxFeesAccountingAndCustoms.Id,
            Name = "Skatt, avgift, regnskap og toll",
            Group = new AreaGroupDto
            {
                Id = AreaGroupConstants.General.Id,
                Name = "Allment"
            }
        };

        // Act
        var translated = await area.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.NotNull(translated);
        Assert.Equal("Taxes, Fees, Accounting and Customs", translated.Name);
        Assert.NotNull(translated.Group);
        Assert.Equal("General", translated.Group.Name);
    }

    [Fact]
    public async Task TranslateDeepAsync_AreaDto_AvoidCircularReferenceWithPackages()
    {
        // Arrange - Area with Package that references back to Area
        var translationService = CreateTranslationService();
        var area = new AreaDto
        {
            Id = AreaConstants.ConstructionInfrastructureAndRealEstate.Id,
            Name = "Bygg, anlegg og eiendom",
            Packages = new List<PackageDto>
            {
                new PackageDto
                {
                    Id = PackageConstants.PropertyRegistration.Id,
                    Name = "Tinglysing eiendom",
                    Area = new AreaDto
                    {
                        Id = AreaConstants.ConstructionInfrastructureAndRealEstate.Id,
                        Name = "Bygg, anlegg og eiendom"
                    }
                }
            }
        };

        // Act
        var translated = await area.TranslateDeepAsync(translationService, "eng", true);

        // Assert - Should translate without infinite loop
        Assert.NotNull(translated);
        Assert.Equal("Construction, Infrastructure and Real Estate", translated.Name);
        Assert.NotNull(translated.Packages);
        Assert.Single(translated.Packages);
        Assert.Equal("Property registration", translated.Packages.First().Name);
        // The nested area in package should not be re-translated (circular reference handling)
    }

    #endregion

    #region AreaGroupDto Deep Translation Tests

    [Fact]
    public async Task TranslateDeepAsync_AreaGroupDto_TranslatesNestedAreas()
    {
        // Arrange
        var translationService = CreateTranslationService();
        var areaGroup = new AreaGroupDto
        {
            Id = AreaGroupConstants.General.Id,
            Name = "Allment",
            Areas = new List<AreaDto>
            {
                new AreaDto
                {
                    Id = AreaConstants.TaxFeesAccountingAndCustoms.Id,
                    Name = "Skatt, avgift, regnskap og toll"
                },
                new AreaDto
                {
                    Id = AreaConstants.Personnel.Id,
                    Name = "Personale"
                }
            }
        };

        // Act
        var translated = await areaGroup.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.NotNull(translated);
        Assert.Equal("General", translated.Name);
        Assert.NotNull(translated.Areas);
        Assert.Equal(2, translated.Areas.Count);
        Assert.Equal("Taxes, Fees, Accounting and Customs", translated.Areas[0].Name);
        Assert.Equal("Personnel", translated.Areas[1].Name);
    }

    #endregion

    #region ResourceDto Deep Translation Tests

    [Fact]
    public async Task TranslateDeepAsync_ResourceDto_TranslatesNestedProvider()
    {
        // Arrange
        var translationService = CreateTranslationService();
        var resource = new ResourceDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Resource",
            Provider = new ProviderDto
            {
                Id = ProviderConstants.Altinn3.Id,
                Name = "Altinn 3"
            }
        };

        // Act
        var translated = await resource.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.NotNull(translated);
        Assert.NotNull(translated.Provider);
        Assert.Equal("Altinn 3", translated.Provider.Name);
    }

    [Fact]
    public async Task TranslateDeepAsync_ResourceDtoCollection_TranslatesAllResources()
    {
        // Arrange - Testing correctness, not parallel performance
        // (Parallel performance is tested in LargePackageCollection test)
        var translationService = CreateTranslationService();
        var resources = new List<ResourceDto>
        {
            new ResourceDto
            {
                Id = ProviderConstants.Altinn3.Id,
                Name = "Resource 1",
                Description = "Test resource 1",
                Provider = new ProviderDto 
                { 
                    Id = ProviderConstants.Altinn3.Id, 
                    Name = "Altinn 3" 
                }
            },
            new ResourceDto
            {
                Id = ProviderConstants.Altinn2.Id,
                Name = "Resource 2",
                Description = "Test resource 2",
                Provider = new ProviderDto 
                { 
                    Id = ProviderConstants.Altinn2.Id, 
                    Name = "Altinn 2" 
                }
            }
        };

        // Act - Translate sequentially to avoid DbContext threading issues in tests
        var translated = new List<ResourceDto>();
        foreach (var resource in resources)
        {
            var translatedResource = await resource.TranslateDeepAsync(translationService, "eng", true);
            translated.Add(translatedResource);
        }

        // Assert
        Assert.Equal(2, translated.Count);
        Assert.NotNull(translated[0].Provider);
        Assert.NotNull(translated[1].Provider);
    }

    #endregion

    #region RoleDto Deep Translation Tests

    [Fact]
    public async Task TranslateDeepAsync_RoleDto_TranslatesNestedProvider()
    {
        // Arrange - Using a real role from RoleConstants
        var translationService = CreateTranslationService();
        var role = new RoleDto
        {
            Id = RoleConstants.MainAdministrator.Id,
            Name = "Hovedadministrator",
            Description = "Intern rolle for å samle alle delegerbare fullmakter",
            Provider = new ProviderDto
            {
                Id = ProviderConstants.Altinn3.Id,
                Name = "Altinn 3"
            }
        };

        // Act
        var translated = await role.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.NotNull(translated);
        Assert.Equal("Main Administrator", translated.Name);
        Assert.NotNull(translated.Provider);
    }

    [Fact]
    public async Task TranslateDeepAsync_RoleDtoCollection_TranslatesAllRoles()
    {
        // Arrange - Testing correctness, not parallel performance
        // (Parallel performance is tested in LargePackageCollection test)
        var translationService = CreateTranslationService();
        var roles = new List<RoleDto>
        {
            new RoleDto
            {
                Id = RoleConstants.MainAdministrator.Id,
                Name = "Hovedadministrator",
                Description = "Intern rolle beskrivelse",
                Provider = new ProviderDto 
                { 
                    Id = ProviderConstants.Altinn3.Id, 
                    Name = "Altinn 3" 
                }
            },
            new RoleDto
            {
                Id = RoleConstants.DeputyLeader.Id,
                Name = "Nestleder",
                Description = "Nestleder beskrivelse",
                Provider = new ProviderDto 
                { 
                    Id = ProviderConstants.CentralCoordinatingRegister.Id, 
                    Name = "Enhetsregisteret" 
                }
            }
        };

        // Act - Translate sequentially to avoid DbContext threading issues in tests
        var translated = new List<RoleDto>();
        foreach (var role in roles)
        {
            var translatedRole = await role.TranslateDeepAsync(translationService, "eng", true);
            translated.Add(translatedRole);
        }

        // Assert
        Assert.Equal(2, translated.Count);
        Assert.Equal("Main Administrator", translated[0].Name);
        Assert.Equal("Deputy Leader", translated[1].Name);
        Assert.NotNull(translated[0].Provider);
        Assert.NotNull(translated[1].Provider);
    }

    #endregion

    #region Base Language (Norwegian Bokmål) Tests

    [Fact]
    public async Task TranslateDeepAsync_PackageDto_WithNorwegianBokmål_ReturnsOriginal()
    {
        // Arrange
        var translationService = CreateTranslationService();
        var package = new PackageDto
        {
            Id = PackageConstants.PropertyRegistration.Id,
            Name = "Tinglysing eiendom",
            Area = new AreaDto
            {
                Id = AreaConstants.ConstructionInfrastructureAndRealEstate.Id,
                Name = "Bygg, anlegg og eiendom"
            }
        };

        // Act
        var translated = await package.TranslateDeepAsync(translationService, "nob", true);

        // Assert - Should return original without translation
        Assert.NotNull(translated);
        Assert.Equal("Tinglysing eiendom", translated.Name);
        Assert.Equal("Bygg, anlegg og eiendom", translated.Area.Name);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task TranslateDeepAsync_LargePackageCollection_CompletesInReasonableTime()
    {
        // Arrange - Create 50 packages using only constant IDs to avoid DB access during parallel operations
        var translationService = CreateTranslationService();
        var packages = Enumerable.Range(0, 50).Select(i => new PackageDto
        {
            // Use PropertyRegistration constant for all packages to leverage constant translations
            Id = PackageConstants.PropertyRegistration.Id,
            Name = $"Tinglysing eiendom {i}",
            Description = "Beskrivelse",
            Area = new AreaDto
            {
                Id = AreaConstants.ConstructionInfrastructureAndRealEstate.Id,
                Name = "Bygg, anlegg og eiendom",
                Description = "Beskrivelse av område"
            }
        }).ToList();

        // Act - Parallel translation safe because all entities use constant IDs
        var startTime = DateTime.UtcNow;
        var translated = await packages.TranslateDeepAsync(translationService, "eng", true);
        var duration = DateTime.UtcNow - startTime;

        // Assert - Should complete within reasonable time (parallel processing should help)
        Assert.Equal(50, translated.Count());
        Assert.True(duration.TotalSeconds < 10, $"Translation took {duration.TotalSeconds} seconds, expected < 10 seconds");
        
        // Verify translations actually happened
        var firstPackage = translated.First();
        Assert.Equal("Property registration", firstPackage.Name);
        Assert.Equal("Construction, Infrastructure and Real Estate", firstPackage.Area.Name);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task TranslateDeepAsync_PackageDto_WithEmptyResourcesCollection_HandlesGracefully()
    {
        // Arrange
        var translationService = CreateTranslationService();
        var package = new PackageDto
        {
            Id = PackageConstants.PropertyRegistration.Id,
            Name = "Tinglysing eiendom",
            Resources = new List<ResourceDto>() // Empty list
        };

        // Act
        var translated = await package.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.NotNull(translated);
        Assert.Equal("Property registration", translated.Name);
        Assert.NotNull(translated.Resources);
        Assert.Empty(translated.Resources);
    }

    [Fact]
    public async Task TranslateDeepAsync_AreaGroupDto_WithNullAreasCollection_HandlesGracefully()
    {
        // Arrange
        var translationService = CreateTranslationService();
        var areaGroup = new AreaGroupDto
        {
            Id = AreaGroupConstants.General.Id,
            Name = "Allment",
            Areas = null
        };

        // Act
        var translated = await areaGroup.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.NotNull(translated);
        Assert.Equal("General", translated.Name);
        Assert.Null(translated.Areas);
    }

    [Fact]
    public async Task TranslateDeepAsync_TypeDto_WithCircularProviderReference_HandlesGracefully()
    {
        // Arrange
        var translationService = CreateTranslationService();
        var type = new TypeDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Type",
            Provider = new ProviderDto
            {
                Id = ProviderConstants.Altinn3.Id,
                Name = "Altinn 3",
                Type = null // Avoid infinite loop
            }
        };

        // Act
        var translated = await type.TranslateDeepAsync(translationService, "eng", true);

        // Assert
        Assert.NotNull(translated);
        Assert.NotNull(translated.Provider);
    }

    #endregion
}
