using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Enums;
using Altinn.AccessManagement.Persistence.Configuration;
using Altinn.AccessManagement.Persistence.Consent;
using Altinn.AccessManagement.Persistence.Policy;
using Altinn.Authorization.ServiceDefaults.Npgsql.Yuniql;
using Altinn.Platform.Register.Enums;
using Azure.Core;
using Azure.Storage;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Altinn.AccessManagement.Persistence.Extensions;

/// <summary>
/// Extension methods for adding access management services to the dependency injection container.
/// </summary>
public static class PersistenceDependencyInjectionExtensions
{
    /// <summary>
    /// Registers access management persistence services with the dependency injection container.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <returns><paramref name="builder"/> for further chaining.</returns>
    public static WebApplicationBuilder AddAccessManagementPersistence(this WebApplicationBuilder builder)
    {
        if (builder.Services.Any(s => s.ServiceType == typeof(Marker)))
        {
            return builder;
        }

        builder.Services.AddSingleton<Marker>();

        builder.Services.AddSingleton<IDelegationChangeEventQueue, DelegationChangeEventQueue>();

        if (builder.Configuration.GetSection("FeatureManagement").GetValue<bool>("UseNewQueryRepo"))
        {
            builder.Services.AddSingleton<IDelegationMetadataRepository, DelegationMetadataRepo>();
            builder.Services.AddSingleton<IResourceMetadataRepository, ResourceMetadataRepo>();
        }
        else
        {
            builder.Services.AddSingleton<IDelegationMetadataRepository, DelegationMetadataRepository>();
            builder.Services.AddSingleton<IResourceMetadataRepository, ResourceMetadataRepository>();
        }

        builder.Services.AddSingleton<IConsentRepository, ConsentRepository>();

        builder.AddDatabase();
        builder.Services.AddDelegationPolicyRepository(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="IPolicyRepository"/> with the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    private static IServiceCollection AddDelegationPolicyRepository(this IServiceCollection services, IConfiguration configuration)
    {
        var config = new AzureStorageConfiguration();

        configuration
            .GetRequiredSection(nameof(AzureStorageConfiguration))
            .Bind(config);

        services.AddDelegationPolicyRepository(options =>
        {
            options.AddRange([
                new(PolicyAccountType.Delegations, config.DelegationsAccountName, config.DelegationsContainer, config.DelegationsBlobEndpoint, config.DelegationsAccountKey, config.BlobLeaseTimeout),
                new(PolicyAccountType.Metadata, config.MetadataAccountName, config.MetadataContainer, config.MetadataBlobEndpoint, config.MetadataAccountKey, config.BlobLeaseTimeout),
                new(PolicyAccountType.ResourceRegister, config.ResourceRegistryAccountName, config.ResourceRegistryContainer, config.ResourceRegistryBlobEndpoint, config.ResourceRegistryAccountKey, config.BlobLeaseTimeout),
            ]);
        });

        return services;
    }

    /// <summary>
    /// Registers a <see cref="IPolicyRepository"/> with the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configureOptions">options for configuring blob service</param>
    /// <returns><paramref name="services"/> for further chaining.</returns>
    private static IServiceCollection AddDelegationPolicyRepository(this IServiceCollection services, Action<List<PolicyOptions>> configureOptions)
    {
        var options = new List<PolicyOptions>();
        configureOptions(options);
        services.AddAzureClients(builder =>
        {
            foreach (var config in options)
            {
                services.AddOptions<PolicyOptions>(config.Account.ToString())
                    .Configure(policy =>
                    {
                        policy.Account = config.Account;
                        policy.AccountName = config.AccountName;
                        policy.Container = config.Container;
                        policy.Key = config.Key;
                        policy.Uri = config.Uri;
                        policy.LeaseAcquireTimeout = config.LeaseAcquireTimeout;
                    })
                    .Validate(policy => !string.IsNullOrEmpty(policy.AccountName), $"{nameof(PolicyOptions.AccountName)} cannot be null or empty for account type {config.Account}")
                    .Validate(policy => !string.IsNullOrEmpty(policy.Container), $"{nameof(PolicyOptions.Container)} cannot be null for account type {config.Account}")
                    .Validate(policy => !string.IsNullOrEmpty(policy.Key), $"{nameof(PolicyOptions.Key)} cannot be null or empty for account type {config.Account} ")
                    .Validate(policy => Uri.IsWellFormedUriString(policy.Uri, UriKind.Absolute), $"{nameof(PolicyOptions.Uri)} cannot be null for account type {config.Account}");

                builder.AddBlobServiceClient(new Uri(config.Uri), new StorageSharedKeyCredential(config.AccountName, config.Key))
                    .WithName(config.Account.ToString())
                    .ConfigureOptions(options =>
                    {
                        options.Retry.Mode = RetryMode.Exponential;
                        options.Retry.MaxRetries = 5;
                        options.Retry.MaxDelay = TimeSpan.FromSeconds(3);
                    });
            }
        });

        services.TryAddSingleton<IPolicyFactory, PolicyFactory>();

        return services;
    }

    private static IHostApplicationBuilder AddDatabase(this IHostApplicationBuilder builder)
    {
        var fs = new ManifestEmbeddedFileProvider(typeof(PersistenceDependencyInjectionExtensions).Assembly, "Migration");

        builder.AddAltinnPostgresDataSource()
            .MapEnum<DelegationChangeType>("delegation.delegationchangetype")
            .MapEnum<UuidType>("delegation.uuidtype")
            .MapEnum<InstanceDelegationMode>("delegation.instancedelegationmode")
            .MapEnum<InstanceDelegationSource>("delegation.instancedelegationsource")
            .MapEnum<ConsentRequestStatusType>("consent.status_type", new EnumNameTranslator<ConsentRequestStatusType>(static value => value switch
            {
                ConsentRequestStatusType.Accepted => "accepted",
                ConsentRequestStatusType.Rejected => "rejected",
                ConsentRequestStatusType.Created => "created",
                ConsentRequestStatusType.Revoked => "revoked",
                ConsentRequestStatusType.Deleted => "deleted",
                ConsentRequestStatusType.Expired => "expired",
                _ => null,
            }))
            .MapEnum<ConsentRequestEventType>("consent.event_type", new EnumNameTranslator<ConsentRequestEventType>(static value => value switch
            {
                ConsentRequestEventType.Accepted => "accepted",
                ConsentRequestEventType.Rejected => "rejected",
                ConsentRequestEventType.Created => "created",
                ConsentRequestEventType.Revoked => "revoked",
                ConsentRequestEventType.Deleted => "deleted",
                ConsentRequestEventType.Expired => "expired",
                ConsentRequestEventType.Used => "used",
                _ => null,
            }))
            .MapEnum<ConsentPortalViewMode>("consent.portal_view_mode", new EnumNameTranslator<ConsentPortalViewMode>(static value => value switch
            {
                ConsentPortalViewMode.Hide => "hide",
                ConsentPortalViewMode.Show => "show",
                _ => null,
            }))
            .AddYuniqlMigrations(cfg =>
            {
                cfg.Workspace = "/";
                cfg.WorkspaceFileProvider = fs;
            });

        return builder;
    }

    private sealed record Marker;

    private sealed class EnumNameTranslator<TEnum>
    : INpgsqlNameTranslator
    where TEnum : struct, Enum
    {
        private readonly ImmutableArray<(string MemberName, string PgName)> _values;

        public EnumNameTranslator(Func<TEnum, string?> resolver)
        {
            var enumValues = Enum.GetValues<TEnum>();
            var builder = ImmutableArray.CreateBuilder<(string MemberName, string PgName)>(enumValues.Length);

            foreach (var enumValue in enumValues)
            {
                var memberName = enumValue.ToString();
                var name = resolver(enumValue);
                if (name is null)
                {
                    ThrowHelper.ThrowInvalidOperationException($"Missing mapping for enum member '{memberName}' in type '{typeof(TEnum).FullName}'");
                }

                builder.Add((memberName, name));
            }

            builder.Sort(static (l, r) => string.CompareOrdinal(l.MemberName, r.MemberName));
            _values = builder.ToImmutable();
        }

        public string TranslateMemberName(string clrName)
        {
            var index = _values.AsSpan().BinarySearch(new MemberNameMatcher(clrName));

            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(clrName));
            }

            return _values[index].PgName;
        }

        public string TranslateTypeName(string clrName)
            => ThrowHelper.ThrowNotSupportedException<string>();
    }

    private readonly struct MemberNameMatcher(string memberName)
        : IComparable<(string MemberName, string PgName)>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo((string MemberName, string PgName) other)
            => string.CompareOrdinal(memberName, other.MemberName);
    }
}
