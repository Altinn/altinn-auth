using System.Web;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Altinn.Authorization.Cli.ServiceBus.Utils;

internal class ServiceBusHandle
{
    public static ServiceBusHandle Create(string connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
            && uri.Scheme == "sb")
        {
            // Connection string is a endpoint URI
            var options = new DefaultAzureCredentialOptions
            {
                ExcludeInteractiveBrowserCredential = false,
            };

            var query = uri.Query.Length > 1 ? HttpUtility.ParseQueryString(uri.Query.Substring(1)) : [];
            if (query.Get("tenantId") is { Length: > 0 } tenantId)
            {
                options.TenantId = tenantId;
            }

            var credential = new DefaultAzureCredential(options);
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
