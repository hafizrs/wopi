using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure
{
    public interface ISecurityHelperService
    {
        string[] GetAllDepartmentLevelStaticRoles();
        string GetRoleByHierarchy();
        int GetRoleByHierarchyRank(List<string> roles = null);
        bool IsAAuthorizedUser();
        bool IsAAdmin();
        bool IsATaskController();
        bool IsAAdminOrTaskConrtroller();
        bool IsAGroupAdminUser();
        bool IsAAdminBUser();
        bool IsADepartmentLevelUser();
        bool IsAPowerUser();
        bool IsAManagementUser();
        bool IsAMpa1User();
        bool IsAMpa2User();
        bool IsAMpaUser();
        string ExtractDepartmentIdFromDepartmentLevelUser();
        string ExtractOrganizationFromOrgLevelUser();
        List<string> ExtractOrganizationIdsFromOrgLevelUser();
        string GenerateOrganizationAdminBRole(string organizationId);
        bool IsAOrganizationAdminB(string organizationId);
        bool HasReadAccess(string[] idsAllowedToRead, string[] rolesAllowedToRead);
        string[] GetAllDepartmentLevelStaticRolesFromCurrentUserRoles();
        string GetOrganizationGeneralAccessRolePrefixFromCurrentUserRoles();
        List<string> GetLoggedInUserOrganizationGeneralAccessDynamicRole();
        string[] GetLoggedInUserDepartmentLevelDynamicRoles();
        bool ArePrimitiveValuesNullOrEmpty(object objValue);
    }
}
