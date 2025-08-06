using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.TwoFactorAuthentication
{
    public interface ITwoFactorAuthenticationService
    {
        Task<string> GenerateCode(string twoFactorId);
        Task<string> GenerateCode(string twoFactorId, string email, string name);
        Task<TwoFAVerifyResponse> VerifyCode(TwoFactorCodeVerifyQuery query);
    }
}
