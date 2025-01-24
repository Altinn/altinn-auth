using System.CommandLine;
using Altinn.Authorization.CLI.Database;
using Spectre.Console;

var root = new RootCommand("App");
var console = AnsiConsole.Create(new()
{
    Ansi = AnsiSupport.Detect,
});

var commands = new List<Command>()
{
    DatabaseCommand.Commands(console),
};

commands.ForEach(root.AddCommand);
await root.InvokeAsync(args);
