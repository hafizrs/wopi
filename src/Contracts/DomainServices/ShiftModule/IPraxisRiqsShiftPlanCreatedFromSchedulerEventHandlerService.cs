using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

public interface IPraxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService
{
    Task InitiateCockpitGenerationService(List<string> shiftPlanIds);
}