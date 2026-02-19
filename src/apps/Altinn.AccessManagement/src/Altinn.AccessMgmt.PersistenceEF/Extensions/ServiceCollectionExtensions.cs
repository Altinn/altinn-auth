using System.Runtime.InteropServices;
using System.Text;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Data;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Host.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly SortedSet<ulong> _sqlHashes = [];
    private static bool _configureTracing = false;

    public static IServiceCollection AddAccessManagementDatabase(this IServiceCollection services, Action<AccessManagementDatabaseOptions> configureOptions)
    {
        var options = new AccessManagementDatabaseOptions(configureOptions);
        _configureTracing = options.AppConnectionString.Contains("database=authorizationdb", StringComparison.OrdinalIgnoreCase);
        ConstantGuard.ConstantIdsAreUnique();
        services.AddScoped<ReadOnlyInterceptor>();
        services.AddScoped<IAuditAccessor, AuditAccessor>();
        services.AddMemoryCache(); // Add memory cache for translation service
        services.AddScoped<ITranslationService, TranslationService>();
        services.AddScoped<ConnectionQuery>();
        services.AddScoped<AppDbContextFactory>();
        services.AddScoped(sp => sp.GetRequiredService<AppDbContextFactory>().CreateDbContext());

        services.AddSingleton<AuditMiddleware>();

        if (options.EnableEFPooling)
        {
            return options.Source switch
            {
                SourceType.App => services.AddPooledDbContextFactory<AppDbContext>((sp, opt) => AddAppDbContext(sp, opt, options)),
                SourceType.Migration => services.AddPooledDbContextFactory<AppDbContext>((sp, opt) => AddMigrationDbContext(sp, opt, options)),
                _ => throw new ArgumentException("Invalid configured source must be either <App, Migration>", nameof(configureOptions)),
            };
        }

        return options.Source switch
        {
            SourceType.App => services.AddDbContextFactory<AppDbContext>((sp, opt) => AddAppDbContext(sp, opt, options)),
            SourceType.Migration => services.AddDbContextFactory<AppDbContext>((sp, opt) => AddMigrationDbContext(sp, opt, options)),
            _ => throw new ArgumentException("Invalid configured source must be either <App, Migration>", nameof(configureOptions)),
        };
    }

    private static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder builder)
    {
        builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        if (_configureTracing)
        {
            builder.ConfigureDataSource((dataSourceBuilder) =>
            {
                dataSourceBuilder.ConfigureTracing(o =>
                {
                    o.EnableFirstResponseEvent(false);
                    o.ConfigureCommandEnrichmentCallback((activity, command) =>
                    {
                        // Remove useless tags
                        activity.SetTag("db.connection_id", null);
                        activity.SetTag("db.connection_string", null);
                        activity.SetTag("db.name", null);
                        activity.SetTag("db.user", null);
                        activity.SetTag("net.peer.ip", null);
                        activity.SetTag("net.peer.name", null);
                        activity.SetTag("net.transport", null);

                        // Change statement tag to hash large queries and log the full query once per application lifetime
                        // Tags get truncated if # > 8192 by some component before they reach the app insights GUI (remember this happens only
                        // once for a particular query in the POD's lifetime), so they are split into multiple tags. Npgsql does currently
                        // not follow the otel standard for tag (span) names, so the npgsql standard is expanded by adding a number
                        // to "db.statement". This name without a number is recognized by app insights and translated to "Command".
                        // The names with a number are shown as custom properties in app insights.
                        var commandSpan = GetCommandTextHash(command.CommandText).AsSpan();
                        int maxTags = 8; // Arbitrary max number of tags. Effective total is maxTags * maxTagLength
                        int maxTagLength = 8192; // Truncation limit observed in the value chain before the app insights GUI
                        for (int i = 0; i < maxTags; i++)
                        {
                            activity.SetTag(
                                $"db.statement{(i > 0 ? i : null)}",
                                commandSpan.Slice(i * maxTagLength, Math.Min(maxTagLength, commandSpan.Length - (i * maxTagLength))).ToString());
                            if ((i + 1) * maxTagLength >= commandSpan.Length)
                            {
                                break;
                            }
                        }

                        if (command.Parameters.Count > 0)
                        {
                            activity.AddTag("db.command.parameters", GetParametersForLogging(command));
                        }
                    });
                });
            });
        }
    }

    private static string GetCommandTextHash(string commandText)
    {
        if (commandText.Length < 1000)
        {
            return commandText;
        }

        ulong hash = XxHash64Utf8(commandText);
        lock (_sqlHashes)
        {
            if (_sqlHashes.Count > 4000)
            {
                // Can't keep track of more than 4000 unique SQL statement hashes
                return commandText;
            }

            if (!_sqlHashes.Contains(hash))
            {
                _sqlHashes.Add(hash);

                // Log the full command text first occurrence of this hash
                return hash + ":" + commandText;
            }
        }

        // Return hash + truncated command text
        return hash + "-" + commandText.Substring(0, 200);
    }

    private static string GetParametersForLogging(Npgsql.NpgsqlCommand command)
    {
        var parameters = new StringBuilder();
        try
        {
            foreach (var parameter in command.Parameters)
            {
                parameters.Append($"{((Npgsql.NpgsqlParameter)parameter).ParameterName}=");
                AppendParameterValueForLogging((Npgsql.NpgsqlParameter)parameter, parameters);
                parameters.Append(';');
            }
        }
        catch (Exception ex)
        {
            return "Could not format parameters: " + ex.Message;
        }

        if (parameters[parameters.Length - 1] == ';')
        {
            parameters.Length--; // Remove trailing ';'
        }

        return parameters.ToString();
    }

    private static void AppendParameterValueForLogging(Npgsql.NpgsqlParameter parameter, StringBuilder parameters)
    {
        if (!parameter.DataTypeName.EndsWith("[]"))
        {
            parameters.Append(MaskSensitiveValue(parameter.Value?.ToString()));
            return;
        }

        int i = 0;
        int maxToLog = 5;
        if (parameter.Value is System.Collections.IEnumerable enumerable)
        {
            foreach (var parameterValue in enumerable)
            {
                parameters.Append($"{MaskSensitiveValue(parameterValue.ToString())}:");
                if (++i >= maxToLog)
                {
                    parameters.Append($"...skip-{enumerable.Cast<object>().Count() - maxToLog}");
                    break;
                }
            }
        }
        else
        {
            throw new NotImplementedException($"Array parameter logging not implemented for type {parameter.Value?.GetType().FullName}");
        }

        if (parameters[parameters.Length - 1] == ':')
        {
            parameters.Length--; // Remove trailing ':'
        }
    }

    private static string MaskSensitiveValue(string value)
    {
        // Currently the only sensitive query parameter access mgmt is SSN
        return value?.Length == 11 && value.All(char.IsDigit) ? value.Substring(0, 6) + "*****" : value;
    }

    private static ulong XxHash64Utf8(string parameter)
    {
        return System.IO.Hashing.XxHash64.HashToUInt64(MemoryMarshal.AsBytes(parameter.AsSpan()));
    }

    private static void AddMigrationDbContext(IServiceProvider sp, DbContextOptionsBuilder options, AccessManagementDatabaseOptions databaseOptions)
    {
        options.UseAsyncSeeding(async (dbcontext, anyChanges, ct) => await StaticDataIngest.IngestAll((AppDbContext)dbcontext, ct));
        options.UseNpgsql(databaseOptions.MigrationConnectionString, ConfigureNpgsql).ReplaceService<IMigrationsSqlGenerator, CustomMigrationsSqlGenerator>();
    }

    private static void AddAppDbContext(IServiceProvider sp, DbContextOptionsBuilder options, AccessManagementDatabaseOptions databaseOptions)
    {
        options.UseNpgsql(databaseOptions.AppConnectionString, ConfigureNpgsql);
    }

    public class AccessManagementDatabaseOptions
    {
        public AccessManagementDatabaseOptions(Action<AccessManagementDatabaseOptions> configureOptions)
        {
            configureOptions(this);
        }

        public SourceType Source { get; set; } = SourceType.App;

        public bool EnableEFPooling { get; set; } = false;

        public string MigrationConnectionString { get; set; } = string.Empty;

        public string AppConnectionString { get; set; } = string.Empty;
    }
}
