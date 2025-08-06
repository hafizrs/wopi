using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface ITaskManagementService
    {
        void AddTaskScheduleRowLevelSecurity(string taskScheduleId, string clientId);
        void AddTaskSummaryRowLevelSecurity(string taskSummaryId, string clientId);
        TaskSummary GetTaskSummary(string itemId);
        Task<List<TaskSummary>> GetTaskSummarys(List<string> taskSummaryIds);
        Task<bool> UpdateTask(dynamic updateModel);
        Task<bool> RemoveTask(dynamic updateModel);
    }
}
