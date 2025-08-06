using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.PraxisClientModule;
using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Commands.ClientModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ClientModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ClientModule
{
    public interface IPraxisClientMalfunctionGroupService
    {
        Task CreateMalfunctionGroupAsync(CreateMalfunctionGroupCommand command);
        Task UpdateMalfunctionGroupAsync(UpdateMalfunctionGroupCommand command);
        Task UpdateStatusMalfunctionGroupAsync(UpdateStatusMalfunctionGroupCommand command);
        Task DeleteMalfunctionGroupsAsync(DeleteMalfunctionGroupCommand command);
        Task<PraxisClientMalfunctionGroupResponse> GetMalfunctionGroupAsync(string malfunctionId);
        Task<List<PraxisClientMalfunctionGroupResponse>> GetMalfunctionGroupsAsync(GetMalfunctionGroupQuery query);
    }
}
