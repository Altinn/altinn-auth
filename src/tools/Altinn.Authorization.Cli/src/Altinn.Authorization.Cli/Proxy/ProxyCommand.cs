using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Altinn.Authorization.Cli.Utils;
using Altinn.Authorization.ServiceDefaults.HttpClient.MaskinPorten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using Spectre.Console.Cli;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Altinn.Authorization.Cli.Proxy;

[ExcludeFromCodeCoverage]
public class ProxyCommand(CancellationToken cancellationToken)
    : BaseCommand<ProxyCommand.Settings>(cancellationToken)
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(settings.SireEndpoint) &&
            string.IsNullOrEmpty(settings.SireEventsEndpoint) &&
            string.IsNullOrEmpty(settings.FregEndpoint))
        {
            AnsiConsole.MarkupLine("[red]At least one of --sire, --sire-events or --freg must be specified.[/]");
            return -1;
        }

        if (string.IsNullOrEmpty(settings.MaskinportenKey) ||
            string.IsNullOrEmpty(settings.MaskinportenEndpoint) ||
            string.IsNullOrEmpty(settings.MaskinportenClientId) ||
            string.IsNullOrEmpty(settings.MaskinportenScope))
        {
            AnsiConsole.MarkupLine("[red]All MaskinPorten options are required when MaskinPorten is enabled.[/]");
            return -1;
        }

        var builder = WebApplication.CreateBuilder();
        var proxyBuilder = builder.Services.AddReverseProxy();

        List<RouteConfig> routes = [];
        List<ClusterConfig> clusters = [];

        if (!string.IsNullOrEmpty(settings.SireEndpoint))
        {
            AnsiConsole.MarkupLineInterpolated($"[green]Adding SIRE endpoint:[/] [blue]{settings.SireEndpoint}[/]");
            AddEndpoint("sire", settings.SireEndpoint, routes, clusters);
        }

        if (!string.IsNullOrEmpty(settings.SireEventsEndpoint))
        {
            AnsiConsole.MarkupLineInterpolated($"[green]Adding SIRE Events endpoint:[/] [blue]{settings.SireEventsEndpoint}[/]");
            AddEndpoint("sire-events", settings.SireEventsEndpoint, routes, clusters);
        }

        if (!string.IsNullOrEmpty(settings.FregEndpoint))
        {
            AnsiConsole.MarkupLineInterpolated($"[green]Adding FREG endpoint:[/] [blue]{settings.FregEndpoint}[/]");
            AddEndpoint("freg", settings.FregEndpoint, routes, clusters);
        }

        proxyBuilder.LoadFromMemory(
            routes: routes,
            clusters: clusters);

        builder.Services.AddMaskinPortenClient();
        builder.Configuration.AddInMemoryCollection([
            new("Altinn:MaskinPorten:Endpoint", settings.MaskinportenEndpoint),
            new("Altinn:MaskinPorten:Clients:proxy:ClientId", settings.MaskinportenClientId),
            new("Altinn:MaskinPorten:Clients:proxy:Scope", settings.MaskinportenScope),
            new("Altinn:MaskinPorten:Clients:proxy:Key", settings.MaskinportenKey),
        ]);

        proxyBuilder.AddTransforms(builderCtx =>
        {
            builderCtx.AddRequestTransform(async ctx =>
            {
                var client = ctx.HttpContext.RequestServices.GetRequiredService<IMaskinPortenClient>();
                var token = await client.GetAccessToken("proxy", ctx.CancellationToken);
                ctx.ProxyRequest.Headers.Authorization = new("Bearer", token.AccessToken);
            });
        });

        var app = builder.Build();
        app.MapReverseProxy();

        Listen(app, checked((ushort)settings.Port));
        try
        {
            await app.StartAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            LogAddress(app, routes.AsReadOnly());

            await app.WaitForShutdownAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }
        finally
        {
            if (app is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
            else if (app is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return 0;
    }

    private static void AddEndpoint(
        string name,
        string destination,
        List<RouteConfig> routes,
        List<ClusterConfig> clusters)
    {
        routes.Add(new RouteConfig
        {
            RouteId = name,
            ClusterId = name,
            Match = new RouteMatch
            {
                Path = "{**catch-all}",
                Hosts = [$"{name}.localhost"],
            },
        });

        clusters.Add(new ClusterConfig
        {
            ClusterId = name,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                [name] = new DestinationConfig
                {
                    Address = destination,
                },
            },
        });
    }

    private static IServerAddressesFeature GetAddressFeature(WebApplication app)
    {
        var serverFeatures = app.Services.GetRequiredService<IServer>().Features;
        return serverFeatures.Get<IServerAddressesFeature>();
    }

    private static void Listen(WebApplication app, ushort port)
    {
        var addresses = GetAddressFeature(app).Addresses;
        addresses.Clear();
        addresses.Add($"http://[::1]:{port}");
    }

    private static void LogAddress(WebApplication app, IReadOnlyList<RouteConfig> routes)
    {
        var addresses = GetAddressFeature(app).Addresses;
        AnsiConsole.MarkupLineInterpolated($"[green]Proxy is listening on:[/]");
        foreach (var address in addresses)
        {
            AnsiConsole.MarkupLineInterpolated($"- [blue]{address}[/]");
        }

        AnsiConsole.MarkupLine(string.Empty);
        AnsiConsole.MarkupLineInterpolated($"[green]Hosts:[/]");
        foreach (var host in routes.SelectMany(r => r.Match.Hosts).Distinct())
        {
            AnsiConsole.MarkupLineInterpolated($"- [blue]{host}[/]");
        }
    }

    [ExcludeFromCodeCoverage]
    public class Settings
        : BaseCommandSettings
    {
        [Description("Port number to listen on.")]
        [CommandOption("-p|--port <PORT>")]
        [DefaultValue(0)]
        public int Port { get; init; }

        [Description("MaskingPorten Key")]
        [CommandOption("-k|--maskinporten-key <BASE64JWK>")]
        [ExpandEnvironmentVariables]
        public string? MaskinportenKey { get; init; }

        [Description("MaskinPorten Token Endpoint")]
        [CommandOption("-e|--maskinporten-endpoint <URL>")]
        [ExpandEnvironmentVariables]
        public string? MaskinportenEndpoint { get; init; }

        [Description("MaskinPorten Client Id")]
        [CommandOption("-c|--maskinporten-client-id <ID>")]
        [ExpandEnvironmentVariables]
        public string? MaskinportenClientId { get; init; }

        [Description("MaskinPorten Scope")]
        [CommandOption("-s|--maskinporten-scope <SCOPE>")]
        [ExpandEnvironmentVariables]
        public string? MaskinportenScope { get; init; }

        [Description("SIRE endpoint.")]
        [CommandOption("--sire <URL>")]
        [ExpandEnvironmentVariables]
        public string? SireEndpoint { get; init; }

        [Description("SIRE Events endpoint.")]
        [CommandOption("--sire-events <URL>")]
        [ExpandEnvironmentVariables]
        public string? SireEventsEndpoint { get; init; }

        [Description("FREG endpoint.")]
        [CommandOption("--freg <URL>")]
        [ExpandEnvironmentVariables]
        public string? FregEndpoint { get; init; }
    }
}
