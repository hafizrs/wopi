using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.QuickTaskModule;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule
{
    public interface IQuickTaskService
    {
        Task CreateQuickTask(CreateQuickTaskCommand command);
        List<RiqsQuickTaskResponse> GetQuickTasks(string departmentId);
        List<RiqsQuickTask> GetQuickTaskDropdown(string departmentId);
        Task CreateQuickTaskPlan(CreateQuickTaskPlanCommand command);
        List<QuickTaskPlanQueryResponse> GetQuickTaskPlans(GetQuickTaskPlanQuery query);
        bool ValidateQuickTaskInfo(ValidateQuickTaskInfo query);
        bool ValidateQuickTaskPlanInfo(ValidateQuickTaskPlanInfoQuery query);
        RiqsQuickTaskPlanResponse GetQuickTaskPlanById(string id);
        Task UpdateQuickTaskPlan(UpdateQuickTaskPlanCommand command);
        Task UpdateQuickTaskSequence(string[] quickTaskIds);
        Task DeleteQuickTaskPlan(List<string> quickTaskPlansIds);
        Task DeleteQuickTask(string id);
        Task EditQuickTask(EditQuickTaskCommand command);
        void SortQuickTaskPlans(List<QuickTaskPlanQueryResponse> items);
        Task CloneQuickTaskPlan(CloneQuickTaskPlanCommand command);
        Task CloneQuickTaskPlans(CloneQuickTaskPlansCommand command);
        Task UpdateLibraryFormResponse(ObjectArtifact artifact);
        Task DeleteDataForClient(string clientId, string orgId = null);
    }
} 