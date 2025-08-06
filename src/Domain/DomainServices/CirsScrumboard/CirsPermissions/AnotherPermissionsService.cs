using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using System;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumboard.CirsPermissions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard.CirsPermissions
{
    public class AnotherPermissionsService : IPermissionsService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;

        public AnotherPermissionsService(
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService
        )
        {
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
        }

        public Dictionary<string, bool> GetPermissions(CirsDashboardPermission dashboardPermissions)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var permissions = new Dictionary<string, bool>();

            var isAssignAdminKey = CirsModuleConstants.PermissionKeys[$"{CirsPermissionEnum.IS_ASSIGNED_ADMIN}"];
            var canAssignAdminKey = CirsModuleConstants.PermissionKeys[$"{CirsPermissionEnum.CAN_ASSIGN_ADMIN}"];
            var canCreateReportKey = CirsModuleConstants.PermissionKeys[$"{CirsPermissionEnum.CAN_CREATE_REPORT}"];
            var canEditReportKey = CirsModuleConstants.PermissionKeys[$"{CirsPermissionEnum.CAN_EDIT_REPORT}"];
            var canViewReportKey = CirsModuleConstants.PermissionKeys[$"{CirsPermissionEnum.CAN_VIEW_REPORT}"];
            var canInactiveReportKey = CirsModuleConstants.PermissionKeys[$"{CirsPermissionEnum.CAN_INACTIVE_REPORT}"];
            var canSeeActiveCardsKey = CirsModuleConstants.PermissionKeys[$"{CirsPermissionEnum.CAN_SEE_ACTIVE_CARDS}"];
            var canSeeInactiveCardsKey = CirsModuleConstants.PermissionKeys[$"{CirsPermissionEnum.CAN_SEE_INACTIVE_CARDS}"];
            var canGenerateExcelReportKey = CirsModuleConstants.PermissionKeys[$"{CirsPermissionEnum.CAN_GENERATE_EXCEL_REPORT}"];
            var canDeleteReportKey = CirsModuleConstants.PermissionKeys[$"{CirsPermissionEnum.CAN_DELETE_REPORT}"];

            var isAssignedAdmin = IsAAssignedAdmin(dashboardPermissions.AdminIds, securityContext.UserId);
            var canAssignAdmin = CanAssignAdmin(dashboardPermissions.AssignmentLevel);
            var canCreateReport = CanCreateReport(isAssignedAdmin, dashboardPermissions.AssignmentLevel);
            var canEditReport = CanEditReport(isAssignedAdmin, dashboardPermissions.AssignmentLevel);
            var canViewReport = CanViewReport(isAssignedAdmin, dashboardPermissions.AssignmentLevel);
            var canInactiveReport = CanInactiveReport(isAssignedAdmin, dashboardPermissions.AssignmentLevel);
            var canSeeActiveCards = CanSeeActiveCards(isAssignedAdmin, dashboardPermissions.AssignmentLevel);
            var canSeeInactiveCards = CanSeeInactiveCards(isAssignedAdmin, dashboardPermissions.AssignmentLevel);
            var canGenerateExcelReport = CanGenerateExcelReport(isAssignedAdmin, dashboardPermissions.AssignmentLevel);
            var canDeleteReport = CanDeleteReport(isAssignedAdmin, dashboardPermissions.AssignmentLevel);

            permissions.Add(isAssignAdminKey, isAssignedAdmin);
            permissions.Add(canAssignAdminKey, canAssignAdmin);
            permissions.Add(canCreateReportKey, canCreateReport);
            permissions.Add(canEditReportKey, canEditReport);
            permissions.Add(canViewReportKey, canViewReport);
            permissions.Add(canInactiveReportKey, canInactiveReport);
            permissions.Add(canSeeActiveCardsKey, canSeeActiveCards);
            permissions.Add(canSeeInactiveCardsKey, canSeeInactiveCards);
            permissions.Add(canGenerateExcelReportKey, canGenerateExcelReport);
            permissions.Add(canDeleteReportKey, canDeleteReport);

            return permissions;
        }

        public List<string> GetRolesAllowedToRead(string clientId)
        {
            var clientRoles = new List<string>()
            {
                ($"{RoleNames.PowerUser_Dynamic}_{clientId}"),
                ($"{RoleNames.Leitung_Dynamic}_{clientId}"),
                ($"{RoleNames.MpaGroup_Dynamic}_{clientId}")
            };
            return clientRoles;
        }

        private bool IsAAssignedAdmin(IEnumerable<PraxisIdDto> adminIds, string userId)
        {
            return adminIds?.Select(a => a.UserId)?
                        .Contains(userId) ?? false;
        }

        private bool CanAssignAdmin(AssignmentLevel assignmentLevel)
        {
            if (assignmentLevel == AssignmentLevel.Organizational)
            {
                return _securityHelperService.IsAAdminOrTaskConrtroller() || _securityHelperService.IsAGroupAdminUser() || _securityHelperService.IsAAdminBUser();
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithInsight)
            {
                return _securityHelperService.IsAAdminOrTaskConrtroller() ||
                    _securityHelperService.IsAPowerUser();
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithoutInsight)
            {
                return _securityHelperService.IsAAdminOrTaskConrtroller() || _securityHelperService.IsAPowerUser();
            }
            return false;
        }

        private bool CanCreateReport(bool isAssignedAdmin, AssignmentLevel assignmentLevel)
        {
            if (assignmentLevel == AssignmentLevel.Organizational)
            {
                return !_securityHelperService.IsAAdminOrTaskConrtroller();
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithInsight)
            {
                return !_securityHelperService.IsAAdminOrTaskConrtroller();
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithoutInsight)
            {
                return _securityHelperService.IsADepartmentLevelUser() || isAssignedAdmin;
            }
            return false;
        }

        private bool CanEditReport(bool isAssignedAdmin, AssignmentLevel assignmentLevel)
        {
            if (assignmentLevel == AssignmentLevel.Organizational)
            {
                return !_securityHelperService.IsADepartmentLevelUser() && !_securityHelperService.IsAAdminOrTaskConrtroller() || isAssignedAdmin;
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithInsight)
            {
                return !_securityHelperService.IsAMpaUser() && _securityHelperService.IsADepartmentLevelUser() || isAssignedAdmin;
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithoutInsight)
            {
                return isAssignedAdmin || _securityHelperService.IsAPowerUser();
            }
            return false;
        }

        private bool CanViewReport(bool isAssignedAdmin, AssignmentLevel assignmentLevel)
        {
            if (assignmentLevel == AssignmentLevel.Organizational)
            {
                return true;
                //return _securityHelperService.IsAAdminOrTaskConrtroller() ||
                //    _securityHelperService.IsAAdminBUser() || isAssignedAdmin;
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithInsight)
            {
                return true;
                //return _securityHelperService.IsAAdminOrTaskConrtroller() ||
                //    _securityHelperService.IsAAdminBUser() || isAssignedAdmin || _securityHelperService.IsAPowerUser();
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithoutInsight)
            {
                return true;
                //return isAssignedAdmin || _securityHelperService.IsAPowerUser();
            }
            return false;
        }

        private bool CanInactiveReport(bool isAssignedAdmin, AssignmentLevel assignmentLevel)
        {
            if (assignmentLevel == AssignmentLevel.Organizational)
            {
                return isAssignedAdmin || _securityHelperService.IsAGroupAdminUser() || _securityHelperService.IsAAdminBUser();
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithInsight)
            {
                return isAssignedAdmin || _securityHelperService.IsAPowerUser();
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithoutInsight)
            {
                return isAssignedAdmin || _securityHelperService.IsAPowerUser();
            }
            return false;
        }

        private bool CanSeeActiveCards(bool isAssignedAdmin, AssignmentLevel assignmentLevel)
        {
            if (assignmentLevel == AssignmentLevel.Organizational)
            {
                return true;
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithInsight)
            {
                return true;
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithoutInsight)
            {
                return true;
            }
            return false;
        }

        private bool CanSeeInactiveCards(bool isAssignedAdmin, AssignmentLevel assignmentLevel)
        {
            if (assignmentLevel == AssignmentLevel.Organizational)
            {
                return isAssignedAdmin || _securityHelperService.IsAGroupAdminUser() || _securityHelperService.IsAAdminBUser() || _securityHelperService.IsAPowerUser();
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithInsight)
            {
                return isAssignedAdmin || (!_securityHelperService.IsAManagementUser() && _securityHelperService.IsAMpaUser());
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithoutInsight)
            {
                return isAssignedAdmin || _securityHelperService.IsAPowerUser() || _securityHelperService.IsAAdmin();
            }
            return false;
        }

        private bool CanGenerateExcelReport(bool isAssignedAdmin, AssignmentLevel assignmentLevel)
        {
            if (assignmentLevel == AssignmentLevel.Organizational)
            {
                return !_securityHelperService.IsAMpaUser() && !_securityHelperService.IsAManagementUser() || isAssignedAdmin;
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithInsight)
            {
                return !_securityHelperService.IsAMpaUser() || isAssignedAdmin;
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithoutInsight)
            {
                return _securityHelperService.IsAAdmin() || isAssignedAdmin || _securityHelperService.IsAPowerUser();
            }
            return false;
        }

        private bool CanDeleteReport(bool isAssignedAdmin, AssignmentLevel assignmentLevel)
        {
            if (assignmentLevel == AssignmentLevel.Organizational)
            {
                return _securityHelperService.IsAAdminOrTaskConrtroller();
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithInsight)
            {
                return _securityHelperService.IsAAdminOrTaskConrtroller();
            }
            else if (assignmentLevel == AssignmentLevel.UnitWithoutInsight)
            {
                return _securityHelperService.IsAAdminOrTaskConrtroller();
            }
            return false;
        }
    }
}
