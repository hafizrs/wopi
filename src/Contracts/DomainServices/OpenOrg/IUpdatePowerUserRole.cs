using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg
{
    public interface IUpdatePowerUserRole
    {
        Task<bool> UpdateRole(List<PraxisUser> userList, string updateRole, string removeRole);
    }
}
