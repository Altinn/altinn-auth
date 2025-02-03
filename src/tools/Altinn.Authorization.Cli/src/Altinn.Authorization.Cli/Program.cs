using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Altinn.Authorization.Cli.Database;
using Spectre.Console;
using Spectre.Console.Cli;

using var cancellationTokenSource = new CancellationTokenSource();
using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal);
using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);
using var sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, OnSignal);

Console.OutputEncoding = System.Text.Encoding.UTF8;
//AnsiConsole.Console.Profile.Capabilities.Unicode = true;

var app = new CommandApp();

app.Configure(config =>
{
    config.PropagateExceptions();
    config.Settings.Registrar.RegisterInstance(cancellationTokenSource.Token);
    config.AddBranch("db", db =>
    {
        db.AddCommand<ExportDatabaseCommand>("export");
        db.AddCommand<CopyCommand>("cp");
        db.AddCommand<CredentialsCommand>("cred");
        db.AddCommand<BootstapCommand>("bootstrap");
    });
});

try
{
    return await app.RunAsync(args);
}
catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationTokenSource.Token)
{
    AnsiConsole.MarkupLine("[red]The operation was cancelled.[/]");
    return -1;
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return 1;
}

void OnSignal(PosixSignalContext ctx)
{
    ctx.Cancel = true;
    cancellationTokenSource.Cancel();
}

/// <summary>
/// Program entry point.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class Program
{
}
