using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.AbsenceModule;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.AbsenceModule
{
    public interface IAbsenceOverviewService
    {
        // Absence Type operations
        List<RiqsAbsenceTypeResponse> GetAbsenceTypes(GetAbsenceTypesQuery query);
        Task<RiqsAbsenceTypeResponse> GetAbsenceTypeByIdAsync(GetAbsenceTypeByIdQuery query);
        Task CreateAbsenceTypeAsync(CreateAbsenceTypeCommand command);
        Task UpdateAbsenceTypeAsync(UpdateAbsenceTypeCommand command);
        Task DeleteAbsenceTypeAsync(DeleteAbsenceTypeCommand command);

        // Absence Plan operations
        Task<List<RiqsAbsencePlanResponse>> GetAbsencePlansAsync(GetAbsencePlansQuery query);
        Task<RiqsAbsencePlanDetailsResponse> GetAbsencePlanByIdAsync(GetAbsencePlanByIdQuery query);
        Task CreateAbsencePlanAsync(CreateAbsencePlanCommand command);
        Task UpdateAbsencePlanAsync(UpdateAbsencePlanCommand command);
        Task DeleteAbsencePlanAsync(List<string> ids);
        Task UpdateAbsencePlanStatusAsync(UpdateAbsencePlanStatusCommand command);
        
        // Absence Plan Permission operations
        Task<AbsencePlanApprovalPermissionResponse> GetAbsencePlanApprovalPermissionAsync(GetAbsencePlanApprovalPermissionQuery query);
    }
}