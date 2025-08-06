using Aspose.Pdf;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactAuthorizationCheckerService : IObjectArtifactAuthorizationCheckerService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IObjectArtifactPermissionGeneratorService _objectArtifactPermissionGeneratorService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IRepository _repository;
        private readonly IRiqsPediaViewControlService _riqsPediaViewControlService;

        public ObjectArtifactAuthorizationCheckerService(
            ISecurityContextProvider securityContextProvider,
            IObjectArtifactPermissionGeneratorService praxisRolePreparationService,
            ISecurityHelperService securityHelperService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IRepository repository,
            IRiqsPediaViewControlService riqsPediaViewControlService
        )
        {
            _securityContextProvider = securityContextProvider;
            _objectArtifactPermissionGeneratorService = praxisRolePreparationService;
            _securityHelperService = securityHelperService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _repository = repository;
            _riqsPediaViewControlService = riqsPediaViewControlService;
        }

        public bool CanApproveObjectArtifact(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null, RiqsPediaViewControlResponse viewControl = null)
        {
            if (artifact == null || IsAAdmin() || IsATaskController())
            {
                return false;
            }
            var libraryPrinciple = LibraryControlMechanismConstant.GetLibraryControlMechanismDataByOrgId(artifact.OrganizationId, controlMechanismDatas)?.ControlMechanismName;
            if (string.IsNullOrEmpty(libraryPrinciple)) return false;

            return libraryPrinciple switch
            {
                LibraryControlMechanismConstant.Standard => CanApproveObjectArtifactForStandardPrinciple(artifact, controlMechanismDatas, viewControl),
                LibraryControlMechanismConstant.FourEyePrinciple => CanApproveObjectArtifactForFourEyePrinciple(artifact, controlMechanismDatas, viewControl),
                LibraryControlMechanismConstant.SixEyePrinciple => CanApproveObjectArtifactForSixEyePrinciple(artifact, controlMechanismDatas, artifactMappingDatas, viewControl),
                _ => false,
            };
        }

        public bool CanActiveInactiveObjectArtifact(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null, List<DmsArtifactUsageReference> usageReferences = null, RiqsPediaViewControlResponse viewControl = null)
        {
            if (artifact == null)
            {
                return false;
            }

            if (
                (IsAAdmin() || IsATaskController() ||
                 IsAAuthorizedAdminB(artifact) || (IsAAuthorizedPoweruser(artifact) && !_objectArtifactUtilityService.IsAOrgLevelArtifact(artifact.MetaData, artifact.ArtifactType)) ||
                 CanApproveObjectArtifact(artifact, controlMechanismDatas, artifactMappingDatas, viewControl)) &&
                !IsArtifactBeingUsed(artifact.ItemId, usageReferences)
            )
            {
                return CanWriteObjectArtifact(artifact);
            }
            return false;
        }

        public bool IsAReapprovedArtifact(IDictionary<string, MetaValuePair> metaData, bool checkReapproveProcess)
        {
            if (metaData == null) return false;
            var approvalStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.APPROVAL_STATUS.ToString()];
            var approvalStatus = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, approvalStatusKey);

            if (approvalStatus != ((int)LibraryFileApprovalStatusEnum.APPROVED).ToString()) return false;

            if (checkReapproveProcess)
            {
                return !IsReapproveProcessStarted(metaData);
            }
            return true;
        }

        public bool IsReapproveProcessStarted(IDictionary<string, MetaValuePair> metaData)
        {
            if (metaData == null) return false;
            var reapproveProcessStartDateKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.NEXT_REAPPROVE_DATE.ToString()];
            if (
                metaData.TryGetValue(reapproveProcessStartDateKey, out MetaValuePair value) &&
                DateTime.TryParse(value?.Value, out DateTime nextReapproveDate)
            )
            {
                return DateTime.UtcNow > nextReapproveDate.ToUniversalTime();
            }
            return false;
        }

        public bool HaveNextReapproveDateKey(IDictionary<string, MetaValuePair> metaData)
        {
            var nextReapproveDateKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.NEXT_REAPPROVE_DATE.ToString()];
            return metaData != null && metaData.ContainsKey(nextReapproveDateKey);
        }

        public bool CanWriteObjectArtifact(ObjectArtifact artifact)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return
                (artifact?.RolesAllowedToWrite?.Count() > 0 && artifact.RolesAllowedToWrite.Any(r => securityContext.Roles.Contains(r))) ||
                (artifact?.IdsAllowedToWrite?.Count() > 0 && artifact.IdsAllowedToWrite.Contains(securityContext.UserId));
        }

        private bool CanApproveObjectArtifactForStandardPrinciple(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, RiqsPediaViewControlResponse viewControl = null)
        {
            if (
                IsAAuthorizedAdminB(artifact) || (IsAAuthorizedPoweruser(artifact) && !_objectArtifactUtilityService.IsAOrgLevelArtifact(artifact.MetaData, artifact.ArtifactType)) 
                || (IsALibraryApprovalAdmin(artifact?.OrganizationId, controlMechanismDatas) && HaveDefaultUserPermission(viewControl))
                || HaveDeptLevelAdminPermission(artifact, controlMechanismDatas)
            )
            {
                return true;
            }
            return false;
        }
        private bool CanApproveObjectArtifactForFourEyePrinciple(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, RiqsPediaViewControlResponse viewControl = null)
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            if (artifact?.OwnerId == userId) return false;

            return IsALibraryApprovalAdmin(artifact?.OrganizationId, controlMechanismDatas) && HaveDefaultUserPermission(viewControl) || HaveDeptLevelAdminPermission(artifact, controlMechanismDatas);
        }

        private bool CanApproveObjectArtifactForSixEyePrinciple(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null, RiqsPediaViewControlResponse viewControl = null)
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var artifactMappingData = artifactMappingDatas?.Find(a => a.ObjectArtifactId == artifact.ItemId) ?? new RiqsObjectArtifactMapping() { ItemId = Guid.NewGuid().ToString() };
            var previousApproverIds = _objectArtifactUtilityService.GetPreviousApproverIdsByInterval(artifact, artifactMappingData);

            if (artifact.OwnerId == userId || (previousApproverIds != null && previousApproverIds.Contains(userId))) return false;

            var isAFolderLevelAdmin = IsAFolderLevelLibraryApprovalAdmin(artifact.ParentId, artifactMappingDatas);
            if (isAFolderLevelAdmin != null) return (bool)isAFolderLevelAdmin;

            return IsALibraryApprovalAdmin(artifact?.OrganizationId, controlMechanismDatas) && HaveDefaultUserPermission(viewControl) || HaveDeptLevelAdminPermission(artifact, controlMechanismDatas);
        }

        public bool IsALibraryApprovalAdmin(string organizationId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null)
        {
            if (string.IsNullOrWhiteSpace(organizationId)) return false;
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var assignedAdmins = _objectArtifactPermissionGeneratorService.GetLibraryApprovalAdminUserIds(organizationId, controlMechanismDatas);
            return assignedAdmins.Contains(userId);
        }

        public bool HaveDeptLevelAdminPermission(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null)
        {
            var artifactDeptId = _objectArtifactUtilityService.GetObjectArtifactDepartmentId(artifact.MetaData);
            var loggedInDeptId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
            return !_objectArtifactUtilityService.IsAOrgLevelArtifact(artifact.MetaData, artifact.ArtifactType) && IsALibraryDeptLevelAdmin(controlMechanismDatas) && artifactDeptId == loggedInDeptId;
        }

        public bool IsALibraryDeptLevelAdmin(List<RiqsLibraryControlMechanism> controlMechanismDatas = null)
        {
            if (!_securityHelperService.IsADepartmentLevelUser()) return false;
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var deptId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
            var assignedAdmins = _objectArtifactPermissionGeneratorService.GetLibraryDeptLevelAdminUserIds(deptId, controlMechanismDatas);
            return assignedAdmins.Contains(userId);
        }

        public bool IsALibraryUploadAdmin(string organizationId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null)
        {
            if (string.IsNullOrWhiteSpace(organizationId)) return false;
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var assignedAdmins = _objectArtifactPermissionGeneratorService.GetLibraryUploadAdminUserIds(organizationId, controlMechanismDatas);
            return assignedAdmins.Contains(userId);
        }

        public bool? IsAFolderLevelLibraryApprovalAdmin(string parentId, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            if (!string.IsNullOrEmpty(parentId))
            {
                var folderApprovalAdminIds = _objectArtifactPermissionGeneratorService.GetLibraryApprovalAdminUserIdsByParentFolderId(parentId, artifactMappingDatas);
                if (folderApprovalAdminIds?.Count > 0)
                {
                    var userId = _securityContextProvider.GetSecurityContext().UserId;
                    return folderApprovalAdminIds.Contains(userId);
                }
            }
            return null;
        }

        public bool IsAAdminOrTaskConrtroller()
        {
            return IsAAdmin() || IsATaskController();
        }

        public bool IsAMpaUser()
        {
            return IsAMpa1User() || IsAMpa2User();
        }

        private bool IsAAdmin()
        {
            return _securityHelperService.IsAAdmin();
        }

        private bool IsATaskController()
        {
            return _securityHelperService.IsATaskController();
        }

        public bool IsAAdminBUser()
        {
            return _securityHelperService.IsAAdminBUser();
        }

        public bool IsAPowerUser()
        {
            return _securityHelperService.IsAPowerUser();
        }

        public bool IsAManagementUser()
        {
            return _securityHelperService.IsAManagementUser();
        }

        private bool IsAMpa1User()
        {
            return _securityHelperService.IsAMpa1User();
        }

        private bool IsAMpa2User()
        {
            return _securityHelperService.IsAMpa2User();
        }

        private bool IsAAuthorizedAdminB(ObjectArtifact objectArtifact)
        {
            var adminBRole = _objectArtifactPermissionGeneratorService.GenerateAdminBRole(objectArtifact.OrganizationId);
            return _securityContextProvider.GetSecurityContext().Roles.Contains(adminBRole);
        }

        private bool IsAAuthorizedPoweruser(ObjectArtifact objectArtifact)
        {
            var IsAAuthorizedPoweruser = false;
            var departmentId = _objectArtifactUtilityService.GetObjectArtifactDepartmentId(objectArtifact.MetaData);
            if (!string.IsNullOrWhiteSpace(departmentId))
            {
                var poweruserRole = _objectArtifactPermissionGeneratorService.GeneratePoweruserRole(departmentId);
                IsAAuthorizedPoweruser = _securityContextProvider.GetSecurityContext().Roles.Contains(poweruserRole);
            }

            return IsAAuthorizedPoweruser;
        }

        public bool IsALibraryAuthorityMember(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            return IsAAdminBUser() || IsAPowerUser() || IsALibraryApprovalAdmin(artifact.OrganizationId, controlMechanismDatas) ||
               IsALibraryUploadAdmin(artifact.OrganizationId, controlMechanismDatas) || IsAFolderLevelLibraryApprovalAdmin(artifact.ParentId, artifactMappingDatas) == true
               || HaveDeptLevelAdminPermission(artifact, controlMechanismDatas);
        }

        public bool IsAEditAllowedUser(RiqsObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null, RiqsPediaViewControlResponse viewControl = null)
        {
            return !(
                        IsAAdminOrTaskConrtroller() || (IsAMpaUser() &&
                        !(IsAFolderLevelLibraryApprovalAdmin(artifact.ParentId, artifactMappingDatas) == true) &&
                        !(IsALibraryApprovalAdmin(artifact.OrganizationId, controlMechanismDatas) && HaveDefaultUserPermission(viewControl))) || IsStandardFormFillRestrictedUser(artifact)
                )
                || HaveDeptLevelAdminPermission(artifact, controlMechanismDatas);
        }

        public bool IsAShareAllowedUser(RiqsObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null, RiqsPediaViewControlResponse viewControl = null)
        {
            return
                !(
                    IsAAdminOrTaskConrtroller() || ((IsAManagementUser() || IsAMpaUser()) && 
                    !(IsAFolderLevelLibraryApprovalAdmin(artifact.ParentId, artifactMappingDatas) == true) &&
                    !(IsALibraryApprovalAdmin(artifact.OrganizationId, controlMechanismDatas) && HaveDefaultUserPermission(viewControl)))
                )
                || HaveDeptLevelAdminPermission(artifact, controlMechanismDatas);
        }

        public bool IsAArtifactUploadRestrictedUser(RiqsObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null)
        {
            return
                IsAAdminOrTaskConrtroller() || ((IsAMpaUser() || IsAManagementUser()) && 
                !IsALibraryUploadAdmin(artifact.OrganizationId, controlMechanismDatas));
        }

        public bool IsAFormFillRestrictedUser(RiqsObjectArtifact artifact)
        {
            return IsAAdminOrTaskConrtroller() || IsStandardFormFillRestrictedUser(artifact) ;
        }

        public bool CanMoveObjectArtifact(RiqsObjectArtifact artifact, 
            List<RiqsLibraryControlMechanism> controlMechanismDatas = null, 
            List<RiqsObjectArtifactMapping> artifactMappingDatas = null,
            RiqsPediaViewControlResponse viewControl = null)
        {
            return !(
                IsAAdminOrTaskConrtroller() || (IsAMpaUser() &&
                !(IsAFolderLevelLibraryApprovalAdmin(artifact.ParentId, artifactMappingDatas) == true) &&
                !(IsALibraryApprovalAdmin(artifact.OrganizationId, controlMechanismDatas) && HaveDefaultUserPermission(viewControl)))
           ) || HaveDeptLevelAdminPermission(artifact, controlMechanismDatas);
        }

        public bool IsArtifactBeingUsed(string objectArtifactId)
        {
            var relatedEntity = new List<string>
            {
                EntityName.PraxisOpenItem,
                EntityName.PraxisEquipmentMaintenance,
                EntityName.CirsGenericReport
            };
            var objectArtifacts = _repository
                .GetItems<DmsArtifactUsageReference>(o =>
                    o.ObjectArtifactId == objectArtifactId &&
                    relatedEntity.Contains(o.RelatedEntityName) &&
                    !o.IsMarkedToDelete)?
                .ToList() ?? new List<DmsArtifactUsageReference>();
            var isActive = objectArtifacts
                .Exists(a => a.TaskCompletionInfo is { IsTaskCompleted: false });
            return isActive;
        }

        private bool HaveDefaultUserPermission(RiqsPediaViewControlResponse viewControl)
        {
            if (viewControl == null)
            {
                viewControl = _riqsPediaViewControlService.GetRiqsPediaViewControl().GetAwaiter().GetResult();
            }
            if (!viewControl.IsShowViewState) return true;

            return viewControl.IsAdminViewEnabled;
        }

        private bool IsArtifactBeingUsed(string objectArtifactId, List<DmsArtifactUsageReference> usageReferences)
        {
            return usageReferences?.Exists(a => a.ObjectArtifactId == objectArtifactId && 
                                                a.TaskCompletionInfo is { IsTaskCompleted: false }) == true;
        }

        private bool IsStandardFormFillRestrictedUser(ObjectArtifact artifact)
        {
            var organizationId = artifact.OrganizationId;
            //var libraryRights = GetLibraryRights(organizationId);
            var isStandardFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_STANDARD_FILE.ToString()];
            var isStandardFile = _objectArtifactUtilityService.GetMetaDataValueByKey(artifact.MetaData, isStandardFileKey) == ((int)LibraryBooleanEnum.TRUE).ToString();
            return isStandardFile && !(IsAPowerUser() || IsAManagementUser());
        }

        private List<UserPraxisUserIdPair> GetLibraryRights(string organizationId)
        {
            var organization = _repository.GetItem<PraxisOrganization>(po => po.ItemId == organizationId);
            if (organization == null || string.IsNullOrEmpty(organization.LibraryControlMechanism)) return new List<UserPraxisUserIdPair>();
            var rights = GetRiqsLibraryControlMechanism(organizationId, organization.LibraryControlMechanism);
            var admins = rights?.ApprovalAdmins?.ToList() ?? new List<UserPraxisUserIdPair>();
            admins.AddRange(rights?.UploadAdmins?.ToList() ?? new List<UserPraxisUserIdPair>());
            return admins.Distinct().ToList();
        }

        private RiqsLibraryControlMechanism GetRiqsLibraryControlMechanism(string organizationId, string controlMechanism)
        {
            return _repository.GetItem<RiqsLibraryControlMechanism>(i =>
                            i.OrganizationId.Equals(organizationId) &&
                            i.ControlMechanismName.Equals(controlMechanism)
                            && !i.IsMarkedToDelete
                    );
        }
    }
}