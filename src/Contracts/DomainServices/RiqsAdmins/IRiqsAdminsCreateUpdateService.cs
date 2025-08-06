using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsAdmins
{
    public interface IRiqsAdminsCreateUpdateService
    {
        Task InitiateAdminBUpdateOnNewDepartmentAdd(string orgId);
        Task UpdateAdminBRolesOnOrganzationAdminDeputyAdminChange(List<string> adminBIds, string orgId);
        Task CreateUpdateRiqsGroupAdmin(PraxisUser praxisuser, List<string> userCreatedOrgIds = null);
        Task InitiateGroupAdminUpdateOnNewOrganizationAdd(string orgId, string userId);
        Task<List<PraxisUser>> InitiateGroupAdminUpdateOnNewDepartmentAdd(string deptId, string orgId);
    }
}
