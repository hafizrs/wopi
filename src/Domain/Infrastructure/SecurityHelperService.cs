using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.Infrastructure
{
    public class SecurityHelperService : ISecurityHelperService
    {
        private static readonly string[] departmentLevelRoles =
            new[] { RoleNames.PowerUser, RoleNames.Leitung, RoleNames.MpaGroup1, RoleNames.MpaGroup2 };

        private static readonly string[] departmentLevelDynamicRolePrefixes =
            new[] { RoleNames.PowerUser_Dynamic, RoleNames.Leitung_Dynamic, RoleNames.MpaGroup_Dynamic };

        private static readonly string[] orgLevelDynamicRolePrefixes =
            new[] { RoleNames.AdminB_Dynamic, RoleNames.Organization_Read_Dynamic };

        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ILogger<SecurityHelperService> _logger;

        public SecurityHelperService(
            ILogger<SecurityHelperService> logger,
            ISecurityContextProvider securityContextProvider)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
        }

        public string[] GetAllDepartmentLevelStaticRoles()
        {
            return departmentLevelRoles;
        }

        public string GetRoleByHierarchy()
        {
            var roles = _securityContextProvider.GetSecurityContext().Roles;
            if (roles.Contains(RoleNames.Admin)) return RoleNames.Admin;
            else if (roles.Contains(RoleNames.SystemAdmin)) return RoleNames.SystemAdmin;
            else if (roles.Contains(RoleNames.TaskController)) return RoleNames.TaskController;
            else if (roles.Contains(RoleNames.ExternalUser)) return RoleNames.ExternalUser;
            else if (roles.Contains(RoleNames.GroupAdmin)) return RoleNames.GroupAdmin;
            else if (roles.Contains(RoleNames.AdminB)) return RoleNames.AdminB;
            else if (roles.Contains(RoleNames.PowerUser)) return RoleNames.PowerUser;
            else if (roles.Contains(RoleNames.Leitung)) return RoleNames.Leitung;
            else if (roles.Contains(RoleNames.MpaGroup1)) return RoleNames.MpaGroup1;
            else if (roles.Contains(RoleNames.MpaGroup2)) return RoleNames.MpaGroup2;
            return string.Empty;
        }

        public int GetRoleByHierarchyRank(List<string> roles = null)
        {
            if (roles == null) roles = _securityContextProvider.GetSecurityContext().Roles?.ToList() ?? new List<string>();

            if (roles.Contains(RoleNames.Admin)) return 1;
            else if (roles.Contains(RoleNames.SystemAdmin)) return 1;
            else if (roles.Contains(RoleNames.TaskController)) return 1;
            else if (roles.Contains(RoleNames.GroupAdmin)) return 2;
            else if (roles.Contains(RoleNames.AdminB)) return 3;
            else if (roles.Contains(RoleNames.PowerUser)) return 4;
            else if (roles.Contains(RoleNames.Leitung)) return 5;
            else if (roles.Contains(RoleNames.MpaGroup1)) return 6;
            else if (roles.Contains(RoleNames.MpaGroup2)) return 6;
            else if (roles.Contains(RoleNames.ExternalUser)) return 7;
            return int.MaxValue;
        }

        public bool IsAAuthorizedUser()
        {
            return IsADepartmentLevelUser() || IsAAdminBUser() || IsAAdmin();
        }
        public bool IsAAdminOrTaskConrtroller()
        {
            return IsAAdmin() || IsATaskController();
        }

        public bool IsAAdmin()
        {
            var role = GetRoleByHierarchy();
            return role == RoleNames.Admin;
        }

        public bool IsATaskController()
        {
            var role = GetRoleByHierarchy();
            return role == RoleNames.TaskController;
        }

        public bool IsAGroupAdminUser()
        {
            var role = GetRoleByHierarchy();
            return role == RoleNames.GroupAdmin;
        }

        public bool IsAAdminBUser()
        {
            var role = GetRoleByHierarchy();
            return role == RoleNames.AdminB || IsAGroupAdminUser();
        }

        public bool IsADepartmentLevelUser()
        {
            var role = GetRoleByHierarchy();
            return departmentLevelRoles.Contains(role);
        }

        public bool IsAPowerUser()
        {
            var role = GetRoleByHierarchy();
            return role == RoleNames.PowerUser;
        }

        public bool IsAManagementUser()
        {
            var role = GetRoleByHierarchy();
            return role == RoleNames.Leitung;
        }

        public bool IsAMpaUser()
        {
            return IsAMpa1User() || IsAMpa2User();
        }

        public bool IsAMpa1User()
        {
            var role = GetRoleByHierarchy();
            return role == RoleNames.MpaGroup1;
        }

        public bool IsAMpa2User()
        {
            var role = GetRoleByHierarchy();
            return role == RoleNames.MpaGroup2;
        }

        public string[] GetAllDepartmentLevelStaticRolesFromCurrentUserRoles()
        {
            var roles = _securityContextProvider.GetSecurityContext().Roles;
            var requiredRoles = roles.Intersect(departmentLevelRoles).ToArray();
            return requiredRoles;
        }

        public string GetOrganizationGeneralAccessRolePrefixFromCurrentUserRoles()
        {
            var roles = _securityContextProvider.GetSecurityContext().Roles;
            return roles.Any(r => r.Contains(RoleNames.Organization_Read_Dynamic)) ? RoleNames.Organization_Read_Dynamic : string.Empty;
        }

        public List<string> GetLoggedInUserOrganizationGeneralAccessDynamicRole()
        {
            var roles = _securityContextProvider.GetSecurityContext().Roles;
            return roles.Where(r => r.Contains(RoleNames.Organization_Read_Dynamic) || r.Contains(RoleNames.AdminB_Dynamic))?.ToList() ?? new List<string>();
        }

        public string[] GetLoggedInUserDepartmentLevelDynamicRoles()
        {
            var roles = _securityContextProvider.GetSecurityContext().Roles;
            return roles.Where(r => departmentLevelDynamicRolePrefixes.Any(rp => r.Contains(rp)))?.Distinct()?.ToArray();
        }

        public string ExtractDepartmentIdFromDepartmentLevelUser()
        {
            var roles = _securityContextProvider.GetSecurityContext().Roles;
            var role = roles.FirstOrDefault(r => departmentLevelDynamicRolePrefixes.Any(rp => r.Contains(rp)));

            var departmentId = role != null ? role.Split('_')[1] : string.Empty;
            return departmentId;
        }

        public string ExtractOrganizationFromOrgLevelUser()
        {
            var roles = _securityContextProvider.GetSecurityContext().Roles;
            var role = roles.FirstOrDefault(r => orgLevelDynamicRolePrefixes.Any(rp => r.Contains(rp)));

            var organizationId = role != null ? role.Split('_')[1] : string.Empty;
            return organizationId;
        }

        public List<string> ExtractOrganizationIdsFromOrgLevelUser()
        {
            if (IsAGroupAdminUser())
            {
                var roles = _securityContextProvider.GetSecurityContext().Roles;
                var orgIds = roles.Where(r => orgLevelDynamicRolePrefixes.Any(rp => r.Contains(rp)))
                                    .Where(role => role != null)
                                    .Select(role => role.Split('_')[1])
                                    .ToList();

                return orgIds.Distinct().ToList();
            }
            else if (IsAAdminBUser() || IsADepartmentLevelUser())
            {
                var orgId = ExtractOrganizationFromOrgLevelUser();
                if (!string.IsNullOrEmpty(orgId)) return new List<string> { orgId };
            }
            return new List<string>();
        }

        public string GenerateOrganizationAdminBRole(string organizationId)
        {
            var adminbRole = $"{RoleNames.AdminB_Dynamic}_{organizationId}";

            return adminbRole;
        }

        public bool IsAOrganizationAdminB(string organizationId)
        {
            var adminBRole = GenerateOrganizationAdminBRole(organizationId);
            return _securityContextProvider.GetSecurityContext().Roles.Contains(adminBRole);
        }

        public bool HasReadAccess(string[] idsAllowedToRead, string[] rolesAllowedToRead)
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var roles = _securityContextProvider.GetSecurityContext().Roles;

            return idsAllowedToRead.Contains(userId) || rolesAllowedToRead.Any(r => roles.Contains(r));
        }

        public bool ArePrimitiveValuesNullOrEmpty(object objValue)
        {
            try
            {
                if (objValue == null)
                    return true;

                var type = objValue.GetType();

                if (type == null || type.IsPrimitive || type.IsEnum || type == typeof(DateTime))
                {
                    return objValue == null;
                }
                else if (type == typeof(string)) return string.IsNullOrEmpty(objValue?.ToString());
                else if (type.IsArray || type.IsGenericType)
                {
                    if (objValue is IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            return false;
                        }
                        return true;
                    }
                }

                var properties = type.GetProperties();
                foreach (var property in properties)
                {
                    var propValue = property?.GetValue(objValue, null);
                    if (!ArePrimitiveValuesNullOrEmpty(propValue))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured in ArePrimitiveValuesNullOrEmpty -> {ex.Message} -> {ex.StackTrace}");
                return false;
            }
        }

    }
}