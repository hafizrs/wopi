using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.OpenOrg
{
    public interface IUpdateDeletePermissionForOpenOrg
    {
        Task<(bool, List<PraxisUser>)> UpdatePermission(string clientId, bool IsOpenOrganization);
    }
}
