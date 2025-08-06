using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

public interface ICockpitTaskRemoveEventHandlerService
{
    Task InitiateCockpitTaskRemoveEvent();
}