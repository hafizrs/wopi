using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation
{
    public interface IPrepareNavigationRoleByOrganization
    {
        Task<bool> PrepareRole(string roleName, string organizationId);
    }
}
