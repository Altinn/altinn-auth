using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Spectre.Console;
using Spectre.Console.Cli;

using Database = Altinn.Authorization.Cli.Database;
using Register = Altinn.Authorization.Cli.Register;
using ServiceBus=Altinn.Authorization.Cli.ServiceBus;

using var cancellationTokenSource = new CancellationTokenSource();
using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal);
using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);
using var sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, OnSignal);

Console.OutputEncoding = System.Text.Encoding.UTF8;

var app = new CommandApp();

app.Configure(config =>
{
    config.PropagateExceptions();
    config.Settings.Registrar.RegisterInstance(cancellationTokenSource.Token);
    
    config.AddBranch("db", db =>
    {
        db.SetDescription("Commands for working with databases.");
        db.AddCommand<Database.ExportDatabaseCommand>("export");
        db.AddCommand<Database.CopyCommand>("cp");
        db.AddCommand<Database.CredentialsCommand>("cred");
        db.AddCommand<Database.BootstapCommand>("bootstrap");
    });
    
    config.AddBranch("sb", sb =>
    {
        sb.SetDescription("Commands for working with service bus.");
        sb.AddCommand<ServiceBus.RetryCommand>("retry");
    });

    config.AddBranch("register", register =>
    {
        register.SetDescription("Commands for working with altinn-register.");
        register.AddCommand<Register.RetryA2ImportsCommand>("retry");
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
