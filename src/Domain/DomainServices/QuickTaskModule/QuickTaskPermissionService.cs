using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.QuickTaskModule
{
    public class QuickTaskPermissionService : IQuickTaskPermissionService
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        public QuickTaskPermissionService(IRepository repository, ISecurityContextProvider securityContextProvider)
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
        public bool HasQuickTaskPlanDepartmentPermission(string quickTaskId)
        {
            var quickTask = _repository.GetItem<RiqsQuickTask>(qt => qt.ItemId == quickTaskId);
            if (quickTask == null)
            {
                return false;
            }
            return HasDepartmentPermission(quickTask.DepartmentId);
        }

        public bool HasDepartmentPermissionGetByQuickTaskPlanId(string quickTaskPlanId)
        {
            var quickTaskPlan = _repository.GetItem<RiqsQuickTaskPlan>(sp => sp.ItemId == quickTaskPlanId);
            if (quickTaskPlan == null)
            {
                return false;
            }
            return HasDepartmentPermission(quickTaskPlan.DepartmentId);
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