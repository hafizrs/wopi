using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisOrganizationExistCheckService
    {
        Task<QueryHandlerResponse> CheckOrganizationNameExistance(string organizationName, string organizationId);
    }
}
