using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CurrentStatus
{
    public interface IEntityWiseCurrentStatus
    {
        CurrentStatusResponse DataCount(GetCurrentStatusQuery query);
    }
}
