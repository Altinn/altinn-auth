using System.Data;
using Npgsql;

namespace Altinn.Authorization.Host.Database.Extensions;

public static class DbCommandExtensions
{
    internal static readonly AsyncLocal<string?> CommandName = new();

    public static async Task<NpgsqlDataReader> ExecuteReaderWithSpanNameAsync(this NpgsqlCommand command, CommandBehavior commandBehavior, string spanName, CancellationToken cancellationToken = default)
    {
        var previousValue = CommandName.Value;
        CommandName.Value = spanName;

        try
        {
            return await command.ExecuteReaderAsync(commandBehavior, cancellationToken);
        }
        finally
        {
            CommandName.Value = previousValue;
        }
    }
}
