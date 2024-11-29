using System;
using System.Collections.Generic;
using System.Security.Claims;

using AltinnCore.Authentication.Constants;

namespace Altinn.Platform.Authorization.IntegrationTests.Util
{
    public static class PrincipalUtil
    {
        private static readonly string AltinnCoreClaimTypesOrg = "urn:altinn:org";
        private static readonly string AltinnCoreClaimTypesOrgNumber = "urn:altinn:orgNumber";

        public static string GetToken(int userId, int partyId, int authenticationLevel = 2)
        {
            List<Claim> claims = new List<Claim>();
            string issuer = "www.altinn.no";
            claims.Add(new Claim(AltinnCoreClaimTypes.UserId, userId.ToString(), ClaimValueTypes.String, issuer));
            claims.Add(new Claim(AltinnCoreClaimTypes.UserName, "UserOne", ClaimValueTypes.String, issuer));
            claims.Add(new Claim(AltinnCoreClaimTypes.PartyID, partyId.ToString(), ClaimValueTypes.Integer32, issuer));
            claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticateMethod, "Mock", ClaimValueTypes.String, issuer));
            claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, authenticationLevel.ToString(), ClaimValueTypes.Integer32, issuer));

            ClaimsIdentity identity = new ClaimsIdentity("mock");
            identity.AddClaims(claims);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            string token = JwtTokenMock.GenerateToken(principal, new TimeSpan(1, 1, 1));

            return token;
        }

        public static string GetOrgToken(string org, int orgNumber = 111111111, string scope = "altinn:appdeploy")
        {
            List<Claim> claims = new List<Claim>();
            string issuer = "www.altinn.no";
            claims.Add(new Claim(AltinnCoreClaimTypesOrg, org, ClaimValueTypes.String, issuer));
            claims.Add(new Claim(AltinnCoreClaimTypesOrgNumber, orgNumber.ToString(), ClaimValueTypes.Integer32, issuer));
            claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticateMethod, "Mock", ClaimValueTypes.String, issuer));
            claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, "3", ClaimValueTypes.Integer32, issuer));
            claims.Add(new Claim("urn:altinn:scope", scope, ClaimValueTypes.String, "maskinporten"));

            ClaimsIdentity identity = new ClaimsIdentity("mock-org");
            identity.AddClaims(claims);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            string token = JwtTokenMock.GenerateToken(principal, new TimeSpan(1, 1, 1));

            return token;
        }

        public static string GetOrgToken(string org, string orgNumber = "991825827", string? scope = null, string[]? prefixes = null)
        {
            ClaimsPrincipal principal = GetClaimsPrincipal(org, orgNumber, scope, prefixes);

            string token = JwtTokenMock.GenerateToken(principal, new TimeSpan(1, 1, 1));

            return token;
        }

        public static ClaimsPrincipal GetClaimsPrincipal(string org, string orgNumber, string? scope = null, string[]? prefixes = null)
        {
            string issuer = "www.altinn.no";

            List<Claim> claims = new List<Claim>();
            if (!string.IsNullOrEmpty(org))
            {
                claims.Add(new Claim(AltinnCoreClaimTypesOrg, org, ClaimValueTypes.String, issuer));
            }

            if (scope != null)
            {
                claims.Add(new Claim("scope", scope, ClaimValueTypes.String, "maskinporten"));
            }

            if (prefixes is { Length: > 0 })
            {
                foreach (string prefix in prefixes)
                {
                    claims.Add(new Claim("consumer_prefix", prefix, ClaimValueTypes.String, "maskinporten"));
                }
            }

            claims.Add(new Claim(AltinnCoreClaimTypes.OrgNumber, orgNumber.ToString(), ClaimValueTypes.Integer32, issuer));
            claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticateMethod, "Mock", ClaimValueTypes.String, issuer));
            claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, "3", ClaimValueTypes.Integer32, issuer));
            claims.Add(new Claim("consumer", GetOrgNoObject(orgNumber)));

            ClaimsIdentity identity = new ClaimsIdentity("mock-org");
            identity.AddClaims(claims);

            return new ClaimsPrincipal(identity);
        }

        public static string GetAccessToken(string appId, string issuer = "www.altinn.no")
        {
            List<Claim> claims = new List<Claim>();
            if (!string.IsNullOrEmpty(appId))
            {
                claims.Add(new Claim("urn:altinn:app", appId, ClaimValueTypes.String, issuer));
            }

            ClaimsIdentity identity = new ClaimsIdentity("mock-org");
            identity.AddClaims(claims);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            string token = JwtTokenMock.GenerateToken(principal, new TimeSpan(1, 1, 1), issuer);

            return token;
        }

        private static string GetOrgNoObject(string orgNo)
        {
            return $"{{ \"authority\":\"iso6523-actorid-upis\", \"ID\":\"0192:{orgNo}\"}}";
        }
    }
}
