using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;

namespace XUnitTests.Helpers
{
    public static class TestHelpers
    {
        public static SecurityContext CreateSecurityContextWithRequestUri(Uri requestUri, List<string>? roles = null)
        {
            roles ??= new List<string> { "admin b" };

            return new SecurityContext(
                "userName",
                roles,
                "tenantId",
                "oauthBearerToken",
                true,
                true,
                "displayName",
                "userId",
                "requestOrigin",
                "siteName",
                "siteId",
                "email",
                "language",
                "phoneNumber",
                "sessionId",
                requestUri,
                "serviceVersion",
                false,
                "tokenHijackingProtectionHash",
                false,
                DateTime.Now,
                "userPreferredLanguage",
                "postLogOutHandlerDataKey",
                "organizationId"
            );
        }
    }
}
