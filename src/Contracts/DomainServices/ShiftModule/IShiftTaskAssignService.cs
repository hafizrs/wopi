using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IShiftTaskAssignService
    {
        Task AssignTasks(RiqsShiftPlan shiftPlan);
        Task UpdateShiftPlanForProcessGuideCreated(List<string> shiftPlanIds, string processGuideId);
    }
}
