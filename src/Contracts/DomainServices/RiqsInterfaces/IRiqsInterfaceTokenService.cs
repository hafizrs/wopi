using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface IRiqsInterfaceTokenService
    {
        Task<ExternalUserTokenResponse> GetInterfaceTokenAsync(string code, string state);
        Task<ExternalUserTokenResponse> GetInterfaceTokenAsync(string refreshTokenId);
        Task<ExternalUserTokenResponse> GetInterfaceTokenAsyncByUserId();
    }
}
