using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule
{
    public interface IQuickTaskAssignService
    {
        Task AssignTasks(RiqsQuickTaskPlan quickTaskPlan);
        Task UpdateQuickTaskPlanForProcessGuideCreated(List<string> quickTaskPlanId, string processGuideId, string formId);
    }
} 