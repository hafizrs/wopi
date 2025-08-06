using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IUserCountMaintainService
    {
        Task InitiateUserCountUpdateProcessOnUserCreate(string departmentId, string organizationId);
        Task UpdateOrganizationLevelUserCount(string departmentId, string organizationId= null);
    }
}
