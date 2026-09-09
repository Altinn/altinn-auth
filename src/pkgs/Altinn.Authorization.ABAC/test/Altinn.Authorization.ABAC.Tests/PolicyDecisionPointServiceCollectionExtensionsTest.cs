using Microsoft.Extensions.DependencyInjection;

namespace Altinn.Authorization.ABAC.Tests;

/// <summary>
/// Tests for the <c>AddPolicyDecisionPoint</c> dependency injection extension: it must
/// register a resolvable <see cref="PolicyDecisionPoint"/> as a singleton and stay
/// idempotent when called more than once.
/// </summary>
[UnitTest]
public class PolicyDecisionPointServiceCollectionExtensionsTest
{
    [Fact]
    public void AddPolicyDecisionPoint_RegistersResolvableSingleton()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddPolicyDecisionPoint()
            .BuildServiceProvider();

        PolicyDecisionPoint first = provider.GetRequiredService<PolicyDecisionPoint>();
        PolicyDecisionPoint second = provider.GetRequiredService<PolicyDecisionPoint>();

        first.Should().NotBeNull();
        second.Should().BeSameAs(first);
    }

    [Fact]
    public void AddPolicyDecisionPoint_CalledTwice_RegistersSingleDescriptor()
    {
        IServiceCollection services = new ServiceCollection()
            .AddPolicyDecisionPoint()
            .AddPolicyDecisionPoint();

        services.Count(descriptor => descriptor.ServiceType == typeof(PolicyDecisionPoint)).Should().Be(1);
    }
}
