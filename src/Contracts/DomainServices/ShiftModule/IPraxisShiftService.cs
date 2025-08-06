using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisShiftService
    {
        Task CreateShift(CreateShiftCommand command);
        List<RiqsShiftResponse> GetShifts(string departmentId);
        List<RiqsShift> GetShiftDropdown(string departmentId);
        Task CreateShiftPlan(CreateShiftPlanCommand command);
        List<ShiftPlanQueryResponse> GetShiftPlans(GetShiftPlanQuery query);
        bool ValidateShiftInfo(ValidateShiftInfo query);
        bool ValidateShiftPlanInfo(ValidateShiftPlanInfoQuery query);
        RiqsShiftPlanResponse GetShiftPlanById(string id);
        Task UpdateShiftPlan(UpdateShiftPlanCommand command);
        Task UpdateShiftSequence(string[] shiftIds);
        Task DeleteShiftPlan(List<string> shiftPlansIds);
        Task DeleteShift(string id);
        Task EditShift(EditShiftCommand command);
        void SortShiftPlans(List<ShiftPlanQueryResponse> items);
        Task CloneShiftPlan(CloneShiftPlanCommand command);
        Task CloneShiftPlans(CloneShiftPlansCommand command);
        Task UpdateLibraryFormResponse(ObjectArtifact artifact);
        Task DeleteDataForClient(string clientId, string orgId = null);
    }
}
