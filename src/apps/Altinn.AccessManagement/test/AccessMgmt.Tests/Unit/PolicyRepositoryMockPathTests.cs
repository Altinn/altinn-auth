using System.Collections.Concurrent;
using Altinn.AccessManagement.TestUtils.Mocks;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.AccessManagement.Tests.Unit;

/// <summary>
/// Pins the containment guard in <see cref="PolicyRepositoryMock"/>. The mock is handed the policy
/// path the request under test produced, so a path holding <c>..</c> could otherwise reach files
/// outside the test data folder. Without these tests the guard could be dropped again without a
/// single test turning red.
/// </summary>
[UnitTest]
public class PolicyRepositoryMockPathTests
{
    /// <summary>
    /// Walks far enough up to leave the test data folder whichever depth it sits at.
    /// </summary>
    private const string EscapingPath = "app_org1_app1/../../../../../escaped-policy.xml";

    private const string NestedPath = "app_org1_app1/20001337/p50006078/delegationpolicy.xml";

    private static PolicyRepositoryMock Repository(string filepath)
    {
        return new PolicyRepositoryMock(filepath, new Mock<ILogger<PolicyRepositoryMock>>().Object, new ConcurrentDictionary<string, byte[]>());
    }

    /// <summary>
    /// Test case: Ask whether a policy exists through a path that leaves the test data folder.
    /// Expected: The lookup is refused instead of reporting on a file outside the folder.
    /// </summary>
    [Fact]
    public async Task PolicyExistsAsync_PathLeavesTestDataFolder_ThrowsArgumentException()
    {
        PolicyRepositoryMock repository = Repository(EscapingPath);

        await Assert.ThrowsAsync<ArgumentException>(async () => await repository.PolicyExistsAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Test case: Read a policy through a path that leaves the test data folder.
    /// Expected: The read is refused instead of returning a file outside the folder.
    /// </summary>
    [Fact]
    public async Task GetPolicyAsync_PathLeavesTestDataFolder_ThrowsArgumentException()
    {
        PolicyRepositoryMock repository = Repository(EscapingPath);

        await Assert.ThrowsAsync<ArgumentException>(async () => await repository.GetPolicyAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Test case: Write a policy through a path that leaves the test data folder.
    /// Expected: The write is refused instead of landing outside the folder.
    /// </summary>
    [Fact]
    public async Task WritePolicyAsync_PathLeavesTestDataFolder_ThrowsArgumentException()
    {
        PolicyRepositoryMock repository = Repository(EscapingPath);
        using MemoryStream content = new MemoryStream("<policy />"u8.ToArray());

        await Assert.ThrowsAsync<ArgumentException>(async () => await repository.WritePolicyAsync(content, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Test case: Ask whether a policy exists through an ordinary nested path.
    /// Expected: The guard lets the path through and the policy in the test data folder is found.
    /// </summary>
    [Fact]
    public async Task PolicyExistsAsync_NestedPathInsideTestDataFolder_FindsPolicy()
    {
        PolicyRepositoryMock repository = Repository(NestedPath);

        bool exists = await repository.PolicyExistsAsync(TestContext.Current.CancellationToken);

        exists.Should().BeTrue();
    }

    /// <summary>
    /// Test case: Read a policy through an ordinary nested path.
    /// Expected: The guard lets the path through and the policy is read.
    /// </summary>
    [Fact]
    public async Task GetPolicyAsync_NestedPathInsideTestDataFolder_ReadsPolicy()
    {
        PolicyRepositoryMock repository = Repository(NestedPath);

        using Stream policy = await repository.GetPolicyAsync(TestContext.Current.CancellationToken);

        policy.Length.Should().BePositive();
    }
}
