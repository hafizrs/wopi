using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Linq;

using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisShiftPermissionService : IPraxisShiftPermissionService
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        public PraxisShiftPermissionService(IRepository repository, ISecurityContextProvider securityContextProvider)
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
        }

        public string[] GetRolesAllowedToRead(string departmentId)
        {
            var organizationId = GetOrganisationId(departmentId);

            var roles = new List<string> { RoleNames.Admin, RoleNames.TaskController };

            roles.Add($"{RoleNames.AdminB_Dynamic}_{organizationId}");
            roles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
            roles.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");
            roles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");

            return roles.ToArray();
        }

        public string[] GetRolesAllowedToUpdate(string departmentId)
        {
            var organizationId = GetOrganisationId(departmentId);
            var roles = new List<string> { RoleNames.Admin, RoleNames.TaskController };

            roles.Add($"{RoleNames.AdminB_Dynamic}_{organizationId}");
            roles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
            roles.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");

            return roles.ToArray();
        }

        public string[] GetRolesAllowedToDelete(string departmentId)
        {
            return GetRolesAllowedToUpdate(departmentId);
        }

        private string GetOrganisationId(string departmentId)
        {
            var organisation = _repository.GetItems<PraxisClient>(o => o.ItemId == departmentId).FirstOrDefault();
            if (organisation == null)
            {
                return string.Empty;
            }
            return organisation.ParentOrganizationId;
        }
        public bool HasShiftPlanDepartmentPermission(string shiftId)
        {
            var shift = _repository.GetItem<RiqsShift>(sft => sft.ItemId == shiftId);
            if (shift == null)
            {
                return false;
            }
            return HasDepartmentPermission(shift.DepartmentId);
        }

        public bool HasDepartmentPermissionGetByShiftplanId(string shiftPlanId)
        {
            var shiftPlan = _repository.GetItem<RiqsShiftPlan>(sp => sp.ItemId == shiftPlanId);
            if (shiftPlan == null)
            {
                return false;
            }
            return HasDepartmentPermission(shiftPlan.Shift.DepartmentId);
        }

        public bool HasDepartmentPermission(string departmentId)
        {
            if (IsSystemUser() || IsAdminUser())
            {
                return true;
            }

            var securityContext = _securityContextProvider.GetSecurityContext();
            var user = _repository.GetItem<PraxisUser>(u => u.UserId == securityContext.UserId);
            var department = user.ClientList.FirstOrDefault(client => client.ClientId == departmentId);
            if (department == null)
            {
                return false;
            }

            return department.Roles.Any(role =>
            role == RoleNames.Admin ||
            role == RoleNames.AdminB ||
            role == RoleNames.PowerUser ||
            role == RoleNames.Leitung);
        }

        private bool IsSystemUser()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return securityContext.Roles.Contains("system_admin");
        }
        private bool IsAdminUser()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return securityContext.Roles.Contains("admin");
        }
    }
}
