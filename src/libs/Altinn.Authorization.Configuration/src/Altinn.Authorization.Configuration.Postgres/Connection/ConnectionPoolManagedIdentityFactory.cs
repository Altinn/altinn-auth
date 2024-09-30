using Altinn.Authorization.Configuration.Postgres.Options;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Altinn.Authorization.Configuration.Postgres.Connection;

/// <summary>
/// Factory class responsible for creating and managing PostgreSQL connection pools 
/// using Azure Managed Identity for authentication. This class checks if the token 
/// has expired and refreshes it as necessary before creating a new connection pool.
/// </summary>
/// <inheritdoc/>
public partial class ConnectionPoolManagedIdentityFactory(ILogger<ConnectionPoolManagedIdentityFactory> logger, IOptions<AltinnPostgresOptions> options) : ConnectionPoolFactory
{
     /// <summary>
     /// An instance of <see cref="TokenIssuer"/> responsible for handling token management 
     /// (including token refreshing) for connecting to PostgreSQL using Azure Managed Identity.
     /// </summary>
     private TokenIssuer TokenHandler { get; } = new(options.Value.TokenCredential);

     /// <summary>
     /// Gets the configured options for the PostgreSQL connection.
     /// </summary>
     private IOptions<AltinnPostgresOptions> Options { get; } = options;

     /// <summary>
     /// Gets the logger instance for logging connection-related events.
     /// </summary>
     private ILogger<ConnectionPoolManagedIdentityFactory> Logger { get; } = logger;

     /// <summary>
     /// Creates a new PostgreSQL connection pool using Azure Managed Identity.
     /// It checks if the token is expired or if the connection pool is null and 
     /// refreshes the token as needed before establishing the connection.
     /// </summary>
     /// <param name="cancellationToken">Token for cancelling the task if needed.</param>
     /// <returns>A task representing the asynchronous operation that returns the connection pool.</returns>
     /// <inheritdoc/>
     public override async Task<NpgsqlDataSource> Create(CancellationToken cancellationToken = default)
     {
          await Semaphore.WaitAsync(cancellationToken);
          try
          {
               if (TokenHandler.IsTokenExpired || ConnectionPool == null)
               {
                    var options = Options.Value;
                    Log.LogNewConnectionPool(Logger, options.Host);
                    await TokenHandler.RefreshToken(cancellationToken);

                    var builder = new NpgsqlConnectionStringBuilder()
                    {
                         Host = options.Host,
                         Database = options.Database,
                         Username = "AzureAd",
                         Password = TokenHandler.AccessToken.Token,
                         SslMode = SslMode.Prefer,
                         MaxAutoPrepare = options.MaxAutoPrepare,
                         AutoPrepareMinUsages = options.AutoPrepareMinUsages,
                    };

                    var datasource = new NpgsqlDataSourceBuilder(builder.ConnectionString);
                    options.DataSourceBuilder(datasource);
                    ConnectionPool = NpgsqlDataSource.Create(builder);
               }

               return ConnectionPool;
          }
          catch (Exception ex)
          {
               var options = Options.Value;
               Log.LogFailedConnectionPool(Logger, ex, options.Host);
               throw;
          }
          finally
          {
               Semaphore.Release();
          }
     }

     /// <summary>
     /// Manages token acquisition and refreshing for Azure Managed Identity.
     /// The token is refreshed when it is expired or about to expire.
     /// </summary>
     public class TokenIssuer
     {
          /// <summary>
          /// Initializes a new instance of <see cref="TokenIssuer"/> using the provided <see cref="TokenCredential"/>.
          /// </summary>
          /// <param name="credential">The Azure credential to use for obtaining access tokens.</param>
          public TokenIssuer(TokenCredential credential)
          {
               Credential = credential;
          }

          /// <summary>
          /// Gets the Azure credential used for token acquisition.
          /// </summary>
          private TokenCredential Credential { get; }

          /// <summary>
          /// Gets or sets the current access token used for database authentication.
          /// </summary>
          public AccessToken AccessToken { get; set; }

          /// <summary>
          /// Checks if the current token is expired.
          /// </summary>
          public bool IsTokenExpired => AccessToken.RefreshOn <= DateTimeOffset.UtcNow;

          /// <summary>
          /// The scopes required for authenticating against the PostgreSQL server using Azure Active Directory.
          /// </summary>
          private static string[] TokenScopes { get; } = ["https://ossrdbms-aad.database.windows.net/.default"];

          /// <summary>
          /// Refreshes the access token asynchronously using the provided Azure credentials.
          /// </summary>
          /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
          internal async Task RefreshToken(CancellationToken cancellationToken) =>
               AccessToken = await Credential.GetTokenAsync(new TokenRequestContext(TokenScopes), cancellationToken);
     }

     /// <summary>
     /// Logs messages related to connection pool creation and failure.
     /// </summary>
     private static partial class Log
     {
          [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Creating new connection pool for host {host} using managed identity")]
          internal static partial void LogNewConnectionPool(ILogger logger, string host);
          
          [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to create connection pool for host {host} using managed identity")]
          internal static partial void LogFailedConnectionPool(ILogger logger, Exception ex, string host);
     }
}
