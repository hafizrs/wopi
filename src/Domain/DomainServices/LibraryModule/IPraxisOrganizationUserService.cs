using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public interface IPraxisOrganizationUserService
    {
        Task<List<PraxisUserResponse>> GetOrganizationUsers(GetPraxisOrganizationUserQuery query);
    }
}
