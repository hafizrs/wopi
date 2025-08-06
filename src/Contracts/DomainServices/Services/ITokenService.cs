using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Signature;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface ITokenService
    {
        Task<string> GetExternalToken(string clientId, string clientSecret, string origin);
        Task<SignatureTokenResponseCommand> GetExternalToken(string clientId, string clientSecret);
        Task<string> GetAdminToken();
        void CreateAdminImpersonateContext(SecurityContext securityContext);
        void CreateImpersonateContext(SecurityContext securityContext ,string email, string userId, List<string> roles);
    }
}
