using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Tests for the TranslationService to ensure proper translation behavior
/// </summary>
public class TranslationServiceTests : IClassFixture<PostgresFixture>
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly TranslationService _translationService;

    public TranslationServiceTests(PostgresFixture fixture)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.SharedDb.Admin.ToString())
            .Options;

        _db = new AppDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _translationService = new TranslationService(_db, _cache);
    }

    #region Norwegian Bokmål (Base Language) Tests

    [Fact]
    public async Task TranslateAsync_WithNorwegianBokmål_ReturnsOriginalWithoutTranslation()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder",
            Description = "Fysisk- eller juridisk person som har ansvaret for den daglige driften i en virksomhet"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "nb");

        // Assert
        Assert.Equal("Daglig leder", result.Name);
        Assert.Equal("Fysisk- eller juridisk person som har ansvaret for den daglige driften i en virksomhet", result.Description);
    }

    [Fact]
    public async Task TranslateAsync_WithNorwegianBokmål_Nob_ReturnsOriginal()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder",
            Description = "Test description"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "nob");

        // Assert
        Assert.Equal("Daglig leder", result.Name);
    }

    [Fact]
    public async Task TranslateAsync_WithNorwegianBokmål_No_ReturnsOriginal()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "no");

        // Assert
        Assert.Equal("Daglig leder", result.Name);
    }

    [Fact]
    public async Task TranslateAsync_WithEmptyLanguageCode_DefaultsToNorwegianBokmål()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "");

        // Assert
        Assert.Equal("Daglig leder", result.Name);
    }

    #endregion

    #region English Translation Tests

    [Fact]
    public async Task TranslateAsync_WithEnglish_ReturnsEnglishFromConstants()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder",
            Description = "Fysisk- eller juridisk person som har ansvaret for den daglige driften i en virksomhet"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "en");

        // Assert
        Assert.Equal("Managing Director", result.Name);
        Assert.Contains("individual or legal entity", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TranslateAsync_WithEnglish_Eng_ReturnsEnglishTranslation()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "eng");

        // Assert
        Assert.Equal("Managing Director", result.Name);
    }

    [Fact]
    public async Task TranslateAsync_WithEnglish_EnUS_ReturnsEnglishTranslation()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "en-US");

        // Assert
        Assert.Equal("Managing Director", result.Name);
    }

    [Fact]
    public async Task TranslateAsync_Package_WithEnglish_ReturnsEnglishFromConstants()
    {
        // Arrange
        var package = new PackageDto
        {
            Id = PackageConstants.Catering.Id,
            Name = "Servering",
            Description = "Skjenkebevillinger og serveringstillatelser"
        };

        // Act
        var result = await _translationService.TranslateAsync(package, "en");

        // Assert
        Assert.Equal("Catering", result.Name);
        Assert.Contains("catering businesses", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Norwegian Nynorsk Translation Tests

    [Fact]
    public async Task TranslateAsync_WithNorwegianNynorsk_ReturnsNynorskFromConstants()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder",
            Description = "Fysisk- eller juridisk person som har ansvaret for den daglige driften i en virksomhet"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "nn");

        // Assert
        Assert.Equal("Dagleg leiar", result.Name);
        Assert.Contains("dagleg", result.Description.ToLower());
    }

    [Fact]
    public async Task TranslateAsync_WithNorwegianNynorsk_Nno_ReturnsNynorskTranslation()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "nno");

        // Assert
        Assert.Equal("Dagleg leiar", result.Name);
    }

    [Fact]
    public async Task TranslateAsync_Package_WithNorwegianNynorsk_ReturnsNynorskFromConstants()
    {
        // Arrange
        var package = new PackageDto
        {
            Id = PackageConstants.Catering.Id,
            Name = "Servering"
        };

        // Act
        var result = await _translationService.TranslateAsync(package, "nn");

        // Assert
        Assert.Equal("Servering", result.Name);
    }

    #endregion

    #region Collection Translation Tests

    [Fact]
    public async Task TranslateCollectionAsync_TranslatesAllItems()
    {
        // Arrange
        var roles = new List<RoleDto>
        {
            new() { Id = RoleConstants.ManagingDirector.Id, Name = "Daglig leder" },
            new() { Id = RoleConstants.Accountant.Id, Name = "Regnskapsfører" }
        };

        // Act
        var result = await _translationService.TranslateCollectionAsync(roles, "en");
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        Assert.Equal("Managing Director", resultList[0].Name);
        Assert.Equal("Accountant", resultList[1].Name);
    }

    [Fact]
    public async Task TranslateCollectionAsync_EmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var roles = new List<RoleDto>();

        // Act
        var result = await _translationService.TranslateCollectionAsync(roles, "en");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task TranslateCollectionAsync_WithNorwegianBokmål_ReturnsOriginals()
    {
        // Arrange
        var roles = new List<RoleDto>
        {
            new() { Id = RoleConstants.ManagingDirector.Id, Name = "Daglig leder" },
            new() { Id = RoleConstants.Accountant.Id, Name = "Regnskapsfører" }
        };

        // Act
        var result = await _translationService.TranslateCollectionAsync(roles, "nb");
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        Assert.Equal("Daglig leder", resultList[0].Name);
        Assert.Equal("Regnskapsfører", resultList[1].Name);
    }

    #endregion

    #region Partial Translation Tests

    [Fact]
    public async Task TranslateAsync_AllowPartialTrue_ReturnsPartialTranslation()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder",
            Description = "Test"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "en", allowPartial: true);

        // Assert - Name should be translated, Description might not match exactly
        Assert.Equal("Managing Director", result.Name);
    }

    [Fact]
    public async Task TranslateAsync_AllowPartialFalse_ReturnsOriginalIfAnyFieldMissing()
    {
        // Arrange - Create a role that doesn't exist in constants
        var role = new RoleDto
        {
            Id = Guid.NewGuid(), // Random ID not in constants
            Name = "Test Role",
            Description = "Test Description"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "en", allowPartial: false);

        // Assert - Should return original since translation failed
        Assert.Equal("Test Role", result.Name);
        Assert.Equal("Test Description", result.Description);
    }

    #endregion

    #region TryTranslate Tests

    [Fact]
    public async Task TryTranslateAsync_WithValidTranslation_ReturnsSuccess()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder"
        };

        // Act
        var (success, result) = await _translationService.TryTranslateAsync(role, "en");

        // Assert
        Assert.True(success);
        Assert.Equal("Managing Director", result.Name);
    }

    [Fact]
    public async Task TryTranslateAsync_WithNorwegianBokmål_ReturnsSuccessWithOriginal()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder"
        };

        // Act
        var (success, result) = await _translationService.TryTranslateAsync(role, "nb");

        // Assert
        Assert.True(success);
        Assert.Equal("Daglig leder", result.Name);
    }

    [Fact]
    public async Task TryTranslateAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = Guid.NewGuid(), // Random ID not in constants or database
            Name = "Test"
        };

        // Act
        var (success, result) = await _translationService.TryTranslateAsync(role, "en");

        // Assert
        Assert.False(success);
        Assert.Equal("Test", result.Name); // Original unchanged
    }

    [Fact]
    public async Task TryTranslateAsync_WithNullSource_ReturnsFalse()
    {
        // Act
        var (success, result) = await _translationService.TryTranslateAsync<RoleDto>(null, "en");

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task TranslateAsync_WithUnknownLanguage_DefaultsToNorwegianBokmål()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder"
        };

        // Act
        var result = await _translationService.TranslateAsync(role, "xyz");

        // Assert
        Assert.Equal("Daglig leder", result.Name);
    }

    [Fact]
    public async Task TranslateAsync_ObjectWithoutId_ReturnsOriginal()
    {
        // Arrange - Create an object type that doesn't have an Id property
        var obj = new { Name = "Test" };

        // Act
        var result = await _translationService.TranslateAsync(obj, "en");

        // Assert
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void Translate_SynchronousVersion_Works()
    {
        // Arrange
        var role = new RoleDto
        {
            Id = RoleConstants.ManagingDirector.Id,
            Name = "Daglig leder"
        };

        // Act
        var result = _translationService.Translate(role, "en");

        // Assert
        Assert.Equal("Managing Director", result.Name);
    }

    #endregion

    #region Database Fallback Tests

    [Fact]
    public async Task UpsertTranslationAsync_AddsNewTranslation()
    {
        // Arrange
        var customId = Guid.NewGuid();
        var translationEntry = new TranslationEntry
        {
            Id = customId,
            Type = "TestType",
            LanguageCode = "eng",
            FieldName = "Name",
            Value = "Test Translation"
        };

        // Act
        await _translationService.UpsertTranslationAsync(translationEntry);

        // Assert
        var saved = await _db.TranslationEntries
            .FirstOrDefaultAsync(t => t.Id == customId && t.LanguageCode == "eng" && t.FieldName == "Name");
        Assert.NotNull(saved);
        Assert.Equal("Test Translation", saved.Value);
    }

    [Fact]
    public async Task UpsertTranslationAsync_UpdatesExistingTranslation()
    {
        // Arrange
        var customId = Guid.NewGuid();
        var translationEntry = new TranslationEntry
        {
            Id = customId,
            Type = "TestType",
            LanguageCode = "eng",
            FieldName = "Name",
            Value = "Original"
        };

        await _translationService.UpsertTranslationAsync(translationEntry);

        // Update the value
        translationEntry.Value = "Updated";

        // Act
        await _translationService.UpsertTranslationAsync(translationEntry);

        // Assert
        var saved = await _db.TranslationEntries
            .FirstOrDefaultAsync(t => t.Id == customId && t.LanguageCode == "eng" && t.FieldName == "Name");
        Assert.NotNull(saved);
        Assert.Equal("Updated", saved.Value);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task TranslateAsync_SecondCall_UsesCachedData()
    {
        // Arrange - Add a custom translation to database
        var customId = Guid.NewGuid();
        var translationEntry = new TranslationEntry
        {
            Id = customId,
            Type = "RoleDto",
            LanguageCode = "eng",
            FieldName = "Name",
            Value = "Cached Translation"
        };
        await _translationService.UpsertTranslationAsync(translationEntry);

        var role = new RoleDto
        {
            Id = customId,
            Name = "Original"
        };

        // Act - First call (should query database)
        var result1 = await _translationService.TranslateAsync(role, "en");
        
        // Reset the object
        role.Name = "Original";
        
        // Second call (should use cache)
        var result2 = await _translationService.TranslateAsync(role, "en");

        // Assert
        Assert.Equal("Cached Translation", result1.Name);
        Assert.Equal("Cached Translation", result2.Name);
    }

    #endregion
}
