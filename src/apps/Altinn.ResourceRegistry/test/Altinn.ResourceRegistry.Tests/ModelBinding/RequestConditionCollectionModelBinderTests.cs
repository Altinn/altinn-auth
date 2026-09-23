#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Tests.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace Altinn.ResourceRegistry.Tests.ModelBinding;

public class RequestConditionCollectionModelBinderTests
    : IClassFixture<RequestConditionCollectionModelBinderTests.Factory>
{
    private readonly Factory _factory;

    public RequestConditionCollectionModelBinderTests(Factory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string, DateTimeOffset> EmptyHeaders_Matches_Anything_Data => new()
    {
        { "1", new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero) },
        { "1", new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero) },
        { "2", new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero) },
    };

    [Theory]
    [MemberData(nameof(EmptyHeaders_Matches_Anything_Data))]
    public async Task EmptyHeaders_Matches_Anything(string version, DateTimeOffset lastModified)
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = true,
            VersionTag = version,
            LastModified = lastModified,
        };

        using var response = await client.PostAsJsonAsync("/", body);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();

        result.Should().Be(nameof(VersionedEntityConditionResult.Succeeded));
    }

    public static TheoryData<string, string, VersionedEntityConditionResult> IfMatches_Data => new()
    {
        { "1", "1", VersionedEntityConditionResult.Succeeded },
        { "1", "2", VersionedEntityConditionResult.Failed },
        { "2", "1", VersionedEntityConditionResult.Failed },
    };

    [Theory]
    [MemberData(nameof(IfMatches_Data))]
    public async Task IfMatch_Works(string ifMatches, string etag, VersionedEntityConditionResult result)
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = true,
            VersionTag = etag,
            LastModified = DateTimeOffset.UnixEpoch,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfMatch.ParseAdd(RequestCondition.SerializeETag(ifMatches));
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(result.ToString());
    }

    public static TheoryData<string, string, VersionedEntityConditionResult> IfNoneMatches_Data => new()
    {
        { "1", "1", VersionedEntityConditionResult.Failed },
        { "1", "2", VersionedEntityConditionResult.Succeeded },
        { "2", "1", VersionedEntityConditionResult.Succeeded },
    };

    [Theory]
    [MemberData(nameof(IfNoneMatches_Data))]
    public async Task IfNoneMatch_Works(string ifNoneMatches, string etag, VersionedEntityConditionResult result)
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = true,
            VersionTag = etag,
            LastModified = DateTimeOffset.UnixEpoch,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfNoneMatch.ParseAdd(RequestCondition.SerializeETag(ifNoneMatches));
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(result.ToString());
    }

    public static TheoryData<DateTimeOffset, DateTimeOffset, VersionedEntityConditionResult> IfUnmodifiedSince_Data = new()
    {
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), VersionedEntityConditionResult.Succeeded },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 01, 01, 01, 01, 01, TimeSpan.Zero), VersionedEntityConditionResult.Succeeded },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2023, 03, 03, 03, 03, 03, TimeSpan.Zero), VersionedEntityConditionResult.Failed },
    };

    [Theory]
    [MemberData(nameof(IfUnmodifiedSince_Data))]
    public async Task IfUnmodifiedSince_Works(DateTimeOffset ifUnmodifiedSince, DateTimeOffset lastModifiedAt, VersionedEntityConditionResult result)
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = true,
            VersionTag = "1",
            LastModified = lastModifiedAt,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfUnmodifiedSince = ifUnmodifiedSince;
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(result.ToString());
    }

    public static TheoryData<DateTimeOffset, DateTimeOffset, VersionedEntityConditionResult> IfModifiedSince_Data = new()
    {
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), VersionedEntityConditionResult.Failed },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 01, 01, 01, 01, 01, TimeSpan.Zero), VersionedEntityConditionResult.Failed },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2023, 03, 03, 03, 03, 03, TimeSpan.Zero), VersionedEntityConditionResult.Succeeded },
    };

    [Theory]
    [MemberData(nameof(IfModifiedSince_Data))]
    public async Task IfModifiedSince_Works(DateTimeOffset ifModifiedSince, DateTimeOffset lastModifiedAt, VersionedEntityConditionResult result)
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = true,
            VersionTag = "1",
            LastModified = lastModifiedAt,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfModifiedSince = ifModifiedSince;
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(result.ToString());
    }

    [Fact]
    public async Task IfUnmodifiedSince_Is_Ignored_If_IfMatch_Are_Present()
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = true,
            VersionTag = "1",
            LastModified = DateTimeOffset.UnixEpoch,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfMatch.ParseAdd(RequestCondition.SerializeETag("1"));
        request.Headers.IfUnmodifiedSince = new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero);
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(nameof(VersionedEntityConditionResult.Succeeded));
    }

    [Fact]
    public async Task IfModifiedSince_Is_Ignored_If_IfNoneMatch_Are_Present()
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = true,
            VersionTag = "1",
            LastModified = new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero),
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfNoneMatch.ParseAdd(RequestCondition.SerializeETag("2"));
        request.Headers.IfModifiedSince = DateTimeOffset.UnixEpoch;
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(nameof(VersionedEntityConditionResult.Succeeded));
    }

    [Fact]
    public async Task Multiple_IfMatch_Conditions_Are_Applied_Using_OR_Semantics()
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = true,
            VersionTag = "1",
            LastModified = DateTimeOffset.UnixEpoch,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfMatch.ParseAdd(RequestCondition.SerializeETag("1"));
        request.Headers.IfMatch.ParseAdd(RequestCondition.SerializeETag("2"));
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(nameof(VersionedEntityConditionResult.Succeeded));
    }

    [Fact]
    public async Task Multiple_IfNoneMatch_Conditions_Are_Applied_Using_OR_Semantics()
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = true,
            VersionTag = "1",
            LastModified = DateTimeOffset.UnixEpoch,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfNoneMatch.ParseAdd(RequestCondition.SerializeETag("1"));
        request.Headers.IfNoneMatch.ParseAdd(RequestCondition.SerializeETag("2"));
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(nameof(VersionedEntityConditionResult.Failed));
    }

    [Fact]
    public async Task IfMatch_Wildcard_With_Exists_True_Succeeds()
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = true,
            VersionTag = "1",
            LastModified = DateTimeOffset.UnixEpoch,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfMatch.ParseAdd("*");
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(nameof(VersionedEntityConditionResult.Succeeded));
    }

    [Fact]
    public async Task IfMatch_Wildcard_With_Exists_False_Fails()
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = false,
            VersionTag = "1",
            LastModified = DateTimeOffset.UnixEpoch,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfMatch.ParseAdd("*");
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(nameof(VersionedEntityConditionResult.Failed));
    }

    [Fact]
    public async Task IfNoneMatch_Wildcard_With_Exists_True_Fails()
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = true,
            VersionTag = "1",
            LastModified = DateTimeOffset.UnixEpoch,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfNoneMatch.ParseAdd("*");
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(nameof(VersionedEntityConditionResult.Failed));
    }

    [Fact]
    public async Task IfNoneMatch_Wildcard_With_Exists_False_Succeeds()
    {
        using var client = _factory.CreateClient();

        var body = new Entity
        {
            Exists = false,
            VersionTag = "1",
            LastModified = DateTimeOffset.UnixEpoch,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Headers.IfNoneMatch.ParseAdd("*");
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().Be(nameof(VersionedEntityConditionResult.Succeeded));
    }

    public class Entity
        : IVersionEquatable<string>
    {
        public required bool Exists { get; init; }

        public required string? VersionTag { get; init; }

        public required DateTimeOffset LastModified { get; init; }

        public bool ModifiedSince(HttpDateTimeHeaderValue other)
        {
            return LastModified > other;
        }

        public bool VersionEquals(string other)
        {
            return VersionTag == other;
        }
    }

    public class TestController
    {
        [HttpPost("/")]
        public string Check(RequestConditionCollection<string> conditions, [FromBody] Entity entity)
        {
                var result = conditions.Validate(entity);

            return result.ToString();
        }
    }

    public class Factory : TestControllerApplicationFactory<TestController>
    {
        protected override WebApplicationBuilder CreateWebApplicationBuilder()
        {
            var builder = base.CreateWebApplicationBuilder();

            builder.Services.Configure<MvcOptions>(options =>
            {
                options.ModelBinderProviders.Insert(0, RequestConditionCollection.ModelBinderProvider.Instance);
            });

            return builder;
        }
    }
}
