using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

public interface IRemoveCockpitTaskForShiftPlanSchedulerEventHandlerService
{
    Task InitiateCockpitRevokedService(List<string> shiftPlanIds);
}