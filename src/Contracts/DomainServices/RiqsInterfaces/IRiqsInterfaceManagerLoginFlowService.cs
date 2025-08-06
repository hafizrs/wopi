using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface IRiqsInterfaceManagerLoginFlowService
    {
        Task<string> GetInterfaceManagerLoginFlow(string provider);
        Task LogOutInterfaceManager(string provider);
        Task<InterfaceManagerLoginDetail> GetInterfaceManagerLoginInfo(string provider);
    }
}
