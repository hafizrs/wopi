using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface IRiqsInterfaceSessionService
    {
        Task AddRefreshTokenSessionAsync(ExternalUserTokenResponse response, string userId);
        Task DeleteRefreshTokenSessionAsync(string userId);
        Task <string> GetRefreshTokenSessionAsync(string userId);
        Task<string> GetRefreshTokenIdAsync(string userId);
        Task<ExternalUserTokenResponse> GetRefreshTokenSessionByRefTokenIdAsync(string refreshtokenId);
    }
}
