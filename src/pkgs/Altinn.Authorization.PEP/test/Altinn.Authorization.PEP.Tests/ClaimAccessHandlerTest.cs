using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Threading.Tasks;

using Altinn.Common.PEP.Authorization;

using Microsoft.AspNetCore.Authorization;

using Xunit;

namespace Altinn.Authorization.PEP.Tests
{
    public class ClaimAccessHandlerTest
    {
        private readonly ClaimAccessHandler _handler;

        public ClaimAccessHandlerTest()
        {
            _handler = new ClaimAccessHandler();
        }

        [Fact]
        public async Task HandleAsync_MatchingClaim_ContextSucceeds()
        {
            // Arrange
            var requirement = new ClaimAccessRequirement("urn:altinn:org", "ttd");
            var claims = new List<Claim> { new Claim("urn:altinn:org", "ttd") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, default(Document));

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        [Fact]
        public async Task HandleAsync_NoMatchingClaim_ContextFails()
        {
            // Arrange
            var requirement = new ClaimAccessRequirement("urn:altinn:org", "ttd");
            var claims = new List<Claim> { new Claim("urn:altinn:org", "skd") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, default(Document));

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.True(context.HasFailed);
        }

        [Fact]
        public async Task HandleAsync_NoClaims_ContextFails()
        {
            // Arrange
            var requirement = new ClaimAccessRequirement("urn:altinn:org", "ttd");
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, default(Document));

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.True(context.HasFailed);
        }

        [Fact]
        public async Task HandleAsync_MatchingTypeWrongValue_ContextFails()
        {
            // Arrange
            var requirement = new ClaimAccessRequirement("urn:altinn:org", "ttd");
            var claims = new List<Claim> { new Claim("urn:altinn:org", "nav") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, default(Document));

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.True(context.HasFailed);
        }

        [Fact]
        public async Task HandleAsync_MultipleClaims_MatchFound_ContextSucceeds()
        {
            // Arrange
            var requirement = new ClaimAccessRequirement("urn:altinn:org", "ttd");
            var claims = new List<Claim>
            {
                new Claim("urn:altinn:authlevel", "3"),
                new Claim("urn:altinn:org", "ttd"),
                new Claim("scope", "altinn:something"),
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, default(Document));

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleAsync_NullUserClaims_ContextFails()
        {
            // Arrange
            var requirement = new ClaimAccessRequirement("urn:altinn:org", "ttd");
            var user = new ClaimsPrincipal();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, default(Document));

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.True(context.HasFailed);
        }
    }
}
