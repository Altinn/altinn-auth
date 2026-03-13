using System.ComponentModel.DataAnnotations;
using Altinn.AccessManagement.Core.Configuration;

namespace AccessMgmt.Tests.Configuration;

/// <summary>
/// Tests for <see cref="ConsentMigrationSettings"/> validation
/// </summary>
public class ConsentMigrationSettingsValidationTests
{
    [Fact]
    public void ValidateDataAnnotations_ValidSettings_Passes()
    {
        // Arrange
        var settings = new ConsentMigrationSettings
        {
            BatchSize = 50,
            ConsentStatus = 3,
            NormalDelayMs = 2000,
            EmptyFeedDelayMs = 60000,
            OnlyExpiredConsents = true
        };

        // Act
        var results = ValidateSettings(settings);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(50001)]
    public void ValidateDataAnnotations_InvalidBatchSize_Fails(int batchSize)
    {
        // Arrange
        var settings = new ConsentMigrationSettings
        {
            BatchSize = batchSize,
            ConsentStatus = 3,
            NormalDelayMs = 2000,
            EmptyFeedDelayMs = 60000
        };

        // Act
        var results = ValidateSettings(settings);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ConsentMigrationSettings.BatchSize)));
    }

    [Theory]
    [InlineData(0)] // Below min
    [InlineData(-1)] // Negative
    [InlineData(4)] // Above max
    public void ValidateDataAnnotations_InvalidConsentStatus_Fails(int status)
    {
        // Arrange
        var settings = new ConsentMigrationSettings
        {
            BatchSize = 50,
            ConsentStatus = status,
            NormalDelayMs = 2000,
            EmptyFeedDelayMs = 60000
        };

        // Act
        var results = ValidateSettings(settings);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ConsentMigrationSettings.ConsentStatus)));
    }

    [Theory]
    [InlineData(99)]// Below min (100ms)
    [InlineData(60001)] // Above max (1 minute)
    public void ValidateDataAnnotations_InvalidNormalDelayMs_Fails(int delay)
    {
        // Arrange
        var settings = new ConsentMigrationSettings
        {
            BatchSize = 50,
            ConsentStatus = 3,
            NormalDelayMs = delay,
            EmptyFeedDelayMs = 60000
        };

        // Act
        var results = ValidateSettings(settings);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ConsentMigrationSettings.NormalDelayMs)));
    }

    [Theory]
    [InlineData(999)] // Below min (1 second)
    [InlineData(300001)] // Above max (5 minutes)
    public void ValidateDataAnnotations_InvalidEmptyFeedDelayMs_Fails(int delay)
    {
        // Arrange
        var settings = new ConsentMigrationSettings
        {
            BatchSize = 50,
            ConsentStatus = 3,
            NormalDelayMs = 2000,
            EmptyFeedDelayMs = delay
        };

        // Act
        var results = ValidateSettings(settings);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ConsentMigrationSettings.EmptyFeedDelayMs)));
    }

    [Fact]
    public void ValidateDataAnnotations_MultipleInvalidProperties_FailsAll()
    {
        // Arrange
        var settings = new ConsentMigrationSettings
        {
            BatchSize = 0,          // Invalid
            ConsentStatus = 0,      // Invalid
            NormalDelayMs = 50,     // Invalid
            EmptyFeedDelayMs = 500 // Invalid
        };

        // Act
        var results = ValidateSettings(settings);

        // Assert
        Assert.Equal(4, results.Count);
    }

    [Fact]
    public void ValidateDataAnnotations_BoundaryValues_Passes()
    {
        // Arrange - Test min and max valid values
        var minSettings = new ConsentMigrationSettings
        {
            BatchSize = 1,
            ConsentStatus = 1,
            NormalDelayMs = 100,      // 0.1 seconds
            EmptyFeedDelayMs = 1000 // 1 second
        };

        var maxSettings = new ConsentMigrationSettings
        {
            BatchSize = 1000,
            ConsentStatus = 3,
            NormalDelayMs = 60000,    // 1 minute
            EmptyFeedDelayMs = 300000 // 5 minutes
        };

        // Act
        var minResults = ValidateSettings(minSettings);
        var maxResults = ValidateSettings(maxSettings);

        // Assert
        Assert.Empty(minResults);
        Assert.Empty(maxResults);
    }

    [Theory]
    [InlineData(59999)] // Below min (1 minute)
    [InlineData(3600001)]// Above max (1 hour)
    public void ValidateDataAnnotations_InvalidFeatureDisabledDelayMs_Fails(int delay)
    {
        // Arrange
        var settings = new ConsentMigrationSettings
        {
            BatchSize = 50,
            ConsentStatus = 3,
            NormalDelayMs = 2000,
            EmptyFeedDelayMs = 60000,
            FeatureDisabledDelayMs = delay
        };

        // Act
        var results = ValidateSettings(settings);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ConsentMigrationSettings.FeatureDisabledDelayMs)));
    }

    private static List<ValidationResult> ValidateSettings(ConsentMigrationSettings settings)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(settings);
        Validator.TryValidateObject(settings, validationContext, validationResults, validateAllProperties: true);
        return validationResults;
    }
}
