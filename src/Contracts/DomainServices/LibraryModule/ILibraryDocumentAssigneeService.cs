using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface ILibraryDocumentAssigneeService
    {
        Task<List<AssignedDepartment>> GetPurposeWiseLibraryAssignees(LibraryDocumentAssigneeQuery query);
    }
}