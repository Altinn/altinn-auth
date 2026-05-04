using Altinn.Authorization.Cli.Utils;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CommunityToolkit.Diagnostics;

namespace Altinn.Authorization.Cli.ServiceBus.Utils;

internal class ServiceBusHandle
{
    public static ServiceBusHandle Create(string connectionString)
    {
        Guard.IsNotNullOrEmpty(connectionString);

        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
            && uri.Scheme == "sb")
        {
            // Connection string is a endpoint URI
            var credential = AzureCredentialHelper.GetCredential(uri);
            var client = new ServiceBusClient(uri.Host, credential);
            var administrationClient = new ServiceBusAdministrationClient(uri.Host, credential);

            return new ServiceBusHandle(client, administrationClient);
        }

        return new ServiceBusHandle(new(connectionString), new(connectionString));
    }

    private readonly ServiceBusClient _client;
    private readonly ServiceBusAdministrationClient _administrationClient;

    private ServiceBusHandle(ServiceBusClient client, ServiceBusAdministrationClient administrationClient)
    {
        _client = client;
        _administrationClient = administrationClient;
    }

    public ServiceBusClient Client => _client;

    public ServiceBusAdministrationClient AdministrationClient => _administrationClient;
}
