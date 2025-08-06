using System.Collections.Generic;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class AuthUtilityService : IAuthUtilityService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly AccessTokenProvider _accessTokenProvider;

        public AuthUtilityService(
            ISecurityContextProvider securityContextProvider,
            AccessTokenProvider accessTokenProvider
        )
        {
            _securityContextProvider = securityContextProvider;
            _accessTokenProvider = accessTokenProvider;
        }

        public async Task<string> GetAdminToken()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var tokenInfo = new TokenInfo
            {
                UserId = "1bb370d7-7d42-4e9a-afde-9382fa96c417",
                TenantId = securityContext.TenantId,
                SiteId = securityContext.SiteId,
                SiteName = securityContext.SiteName,
                Origin = securityContext.RequestOrigin,
                DisplayName = "Kawsar Ahmed",
                UserName = "kawsar.ahmed@selise.ch",
                Language = securityContext.Language,
                PhoneNumber = securityContext.PhoneNumber,
                Roles = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.Tenantadmin }
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }
    }
}