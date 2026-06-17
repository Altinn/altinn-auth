using System;
using System.IO;

namespace Altinn.AccessManagement.TestUtils;

/// <summary>
/// Resolves the directory holding the AccessManagement integration-test data
/// (<c>AccessMgmt.Tests/Data</c>). That data lives in one place but is read by mocks
/// in this shared TestUtils assembly from several test projects, so it is located by
/// walking up from the running assembly's output directory — robust to the build
/// output layout and the process working directory, unlike a fixed <c>..</c> depth
/// or a CWD-relative path.
/// </summary>
public static class TestDataDirectory
{
    /// <summary>The resolved test-data <c>Data</c> directory (computed once per process).</summary>
    public static string Root { get; } = Resolve();

    /// <summary>Combines <see cref="Root"/> with the given path segments.</summary>
    public static string Combine(params string[] paths)
    {
        var all = new string[paths.Length + 1];
        all[0] = Root;
        Array.Copy(paths, 0, all, 1, paths.Length);
        return Path.Combine(all);
    }

    private static string Resolve()
    {
        // "Xacml" exists in the full test-data set but not in the small subset
        // TestUtils itself deploys, so it disambiguates the canonical directory.
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            // The running test project's own output (full Data/** copied alongside).
            var local = Path.Combine(dir.FullName, "Data");
            if (Directory.Exists(Path.Combine(local, "Xacml")))
            {
                return local;
            }

            // Source tree of the canonical test-data project (running from another vertical).
            var shared = Path.Combine(dir.FullName, "AccessMgmt.Tests", "Data");
            if (Directory.Exists(Path.Combine(shared, "Xacml")))
            {
                return shared;
            }
        }

        // Fallback: the test assembly's output directory (AppContext.BaseDirectory).
        return Path.Combine(AppContext.BaseDirectory, "Data");
    }
}
