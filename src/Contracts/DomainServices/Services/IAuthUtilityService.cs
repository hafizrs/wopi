using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IAuthUtilityService
    {
        Task<string> GetAdminToken();
    }
}