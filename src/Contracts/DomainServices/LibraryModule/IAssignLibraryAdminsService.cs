using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IAssignLibraryAdminsService
    {
        Task<bool> AssignLibraryRights(LibraryRightsAssignCommand command);
        Task<bool> AssignLibraryRightsForDepartment(LibraryRightsAssignCommand command);

        Task<RiqsLibraryControlMechanism> GetLibraryRights(LibraryRightsGetQuery query);
        Task<RiqsLibraryControlMechanism> GetLibraryRightsForDepartment(LibraryRightsGetQuery query);
    }
}
