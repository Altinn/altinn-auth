using System.Runtime.CompilerServices;

namespace Altinn.Authorization.Tests.Fixtures;

/// <summary>
/// Makes every test host poll its configuration files instead of watching them.
/// </summary>
/// <remarks>
/// Most decision tests call <c>WithWebHostBuilder</c> per test, so each test
/// builds a host, and each host watches its configuration files
/// (<c>reloadOnChange: true</c> in <c>Program.cs</c>). On Linux every watcher
/// costs an inotify instance, and the hosts live until their class fixture is
/// disposed, so the suite runs past the default limit of 128 per user
/// (ubuntu-26.04 runners, #4927). With this variable set,
/// <c>PhysicalFileProvider</c> creates no <c>FileSystemWatcher</c> at all.
/// A module initializer runs before any test, including those that use a
/// plain <c>WebApplicationFactory</c> rather than <see cref="AuthorizationApiFixture"/>.
/// </remarks>
internal static class PollingFileWatcher
{
    [ModuleInitializer]
    internal static void Enable() =>
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
}
