using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

public interface IRemoveCockpitTaskForQuickTaskPlanSchedulerEventHandlerService
{
    Task InitiateCockpitRevokedService(List<string> quickTaskPlanIds);
} 