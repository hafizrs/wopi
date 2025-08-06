using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisProcessGuideDeleteService
    {
        Task DeleteClonedProcessGuide(string processGuideId);
    }
}