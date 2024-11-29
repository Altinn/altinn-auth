using Altinn.Authorization.DeployApi.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Spectre.Console;

namespace Altinn.Authorization.DeployApi.BootstrapDatabase;

internal sealed class SaveConnectionStringsTask
    : StepTask
{
    private readonly SecretClient _secrets;
    private readonly IReadOnlyDictionary<string, string> _connectionStrings;

    public SaveConnectionStringsTask(SecretClient secrets, IReadOnlyDictionary<string, string> connectionStrings)
    {
        _secrets = secrets;
        _connectionStrings = connectionStrings;
    }

    public override string Name => $"Writing connection string to keyvault";

    public override async Task ExecuteAsync(ProgressTask task, CancellationToken cancellationToken)
    {
        foreach (var (name, connStr) in _connectionStrings)
        {
            await MaybeUpdate(name, connStr, cancellationToken);
        }
    }

    private async Task MaybeUpdate(string name, string connStr, CancellationToken cancellationToken)
    {
        bool update = true;
        try
        {
            var secret = await _secrets.GetSecretAsync(name, cancellationToken: cancellationToken);
            if (string.Equals(connStr, secret.Value.Value, StringComparison.Ordinal))
            {
                update = false;
            }
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "SecretNotFound")
        {
        }

        if (update)
        {
            await _secrets.SetSecretAsync(name, connStr, cancellationToken: cancellationToken);
        }
    }
}
