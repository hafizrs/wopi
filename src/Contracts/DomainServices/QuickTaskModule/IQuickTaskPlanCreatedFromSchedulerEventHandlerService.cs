using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;

public interface IQuickTaskPlanCreatedFromSchedulerEventHandlerService
{
    Task InitiateCockpitGenerationService(List<string> quickTaskPlanIds);
} 