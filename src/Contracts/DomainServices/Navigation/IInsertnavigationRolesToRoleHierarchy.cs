using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation
{
    public interface IInsertNavigationRolesToRoleHierarchy
    {
        Task<bool> InsertRoleHierarchy(string organizationId, string navRole);
    }
}
