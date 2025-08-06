using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactPermissionHelperService : IObjectArtifactPermissionHelperService
    {
        private readonly IRepository _repository;
        private readonly IObjectArtifactPermissionGeneratorService _objectArtifactPermissionGeneratorService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;

        public ObjectArtifactPermissionHelperService(
            IObjectArtifactPermissionGeneratorService objectArtifactPermissionGeneratorService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IRepository repository
        )
        {
            _objectArtifactPermissionGeneratorService = objectArtifactPermissionGeneratorService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _repository = repository;
        }

        public bool IsAAdminBUpload(string userId, string organizationId)
        {
            var userRoles = GetUserRolesById(userId);
            var adminBRole = _objectArtifactPermissionGeneratorService.GenerateAdminBRole(organizationId);
            var isAAdminB = userRoles != null && userRoles.Contains(adminBRole);
            return isAAdminB;
        }

        public bool IsAPowerUserUpload(string userId, string departmentId)
        {
            var userRoles = GetUserRolesById(userId);
            var powerUserRole = _objectArtifactPermissionGeneratorService.GeneratePoweruserRole(departmentId);
            var isAPowerUser = userRoles != null && userRoles.Contains(powerUserRole);
            return isAPowerUser;
        }

        public bool IsALibraryAdminUpload(ObjectArtifact artifact, string userId)
        {
            var organizationId = artifact.OrganizationId;
            var libraryAdmins = _objectArtifactPermissionGeneratorService.GetLibraryApprovalAdminUserIds(organizationId);
            libraryAdmins.AddRange(_objectArtifactPermissionGeneratorService.GetLibraryUploadAdminUserIds(organizationId));
            libraryAdmins.AddRange(_objectArtifactPermissionGeneratorService.GetLibraryApprovalAdminUserIdsByParentFolderId(artifact.ParentId));

            var isALibraryAdmin = libraryAdmins != null && libraryAdmins.Contains(userId);
            return isALibraryAdmin;
        }

        private string[] GetUserRolesById(string id)
        {
            var user = _repository.GetItem<User>(u => u.ItemId == id);
            return user?.Roles;
        }

        public string[] GetOrganizationLevelObjectArtifactRoles(string organizationId)
        {
            var defaultRoles = GetObjectArtifactDefaultRoles().ToList();
            var roles = new List<string>
            {
                _objectArtifactPermissionGeneratorService.GenerateAdminBRole(organizationId)
            };
            var orgLevelRoles = defaultRoles.Union(roles);

            return orgLevelRoles.ToArray();
        }

        public string[] GetDepartmentLevelObjectArtifactRoles(string organizationId, string departmentId)
        {
            var orgLevelRoles = GetOrganizationLevelObjectArtifactRoles(organizationId).ToList();
            var roles = new List<string>
            {
                _objectArtifactPermissionGeneratorService.GeneratePoweruserRole(departmentId)
            };
            var deptLevelRoles = orgLevelRoles.Union(roles);

            return deptLevelRoles.Concat(new string[] {RoleNames.Admin}).Distinct().ToArray();
        }

        public string[] GetOrganizationLevelObjectArtifactFileApproverRoles(string organizationId)
        {
            var roles = new List<string>
            {
                _objectArtifactPermissionGeneratorService.GenerateAdminBRole(organizationId)
            };

            return roles.ToArray();
        }

        public string[] GetDepartmentLevelObjectArtifactFileApproverRoles(string organizationId, string departmentId)
        {
            var roles = new List<string>
            {
                _objectArtifactPermissionGeneratorService.GenerateAdminBRole(organizationId),
                _objectArtifactPermissionGeneratorService.GeneratePoweruserRole(departmentId)
            };

            return roles.ToArray();
        }

        public string[] GetOrganizationLevelObjectArtifactRemoverRoles(string organizationId)
        {
            return GetOrganizationLevelObjectArtifactRoles(organizationId);
        }

        public string[] GetDepartmentLevelObjectArtifactRemoverRoles(string organizationId, PraxisClient department)
        {
            var roles = new string[] { };
            if (department != null)
            {
                var isAAuditSaveDepartment = department.IsOpenOrganization.Value;

                roles =
                    isAAuditSaveDepartment ?
                    GetObjectArtifactDefaultRoles() :
                    GetDepartmentLevelObjectArtifactRoles(organizationId, department.ItemId);
            }

            return roles.Concat(new string[] {RoleNames.Admin}).Distinct().ToArray();
        }

        public string[] GetLibraryAllAdminUserIds(string organizationId, string parentId)
        {
            var ids = _objectArtifactPermissionGeneratorService.GetLibraryApprovalAdminUserIdsByParentFolderId(parentId);
            ids.AddRange(_objectArtifactPermissionGeneratorService.GetLibraryApprovalAdminUserIds(organizationId));
            ids.AddRange(_objectArtifactPermissionGeneratorService.GetLibraryUploadAdminUserIds(organizationId));

            return ids.Distinct().ToArray();
        }

        public string[] GetObjectArtifactAuthorizedIds(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, bool onlyDeptLevel = false)
        {
            var ids = !onlyDeptLevel ? GetLibraryAllAdminUserIds(artifact.OrganizationId, artifact.ParentId)?.ToList() : new List<string>();
            if (!_objectArtifactUtilityService.IsAOrgLevelArtifact(artifact.MetaData, artifact.ArtifactType) && !string.IsNullOrEmpty(artifact.OwnerId))
            {
                ids.Add(artifact.OwnerId);
            }

            if (!_objectArtifactUtilityService.IsAOrgLevelArtifact(artifact.MetaData, artifact.ArtifactType))
            {
                var deptId = _objectArtifactUtilityService.GetObjectArtifactDepartmentId(artifact.MetaData);
                ids.AddRange(_objectArtifactPermissionGeneratorService.GetLibraryDeptLevelAdminUserIds(deptId, controlMechanismDatas));
            }

            return ids.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        }

        public string[] GetOrganizationLevelGeneralAccessRoles(string organizationId)
        {
            var roles = new List<string>
            {
                _objectArtifactPermissionGeneratorService.GenerateOrganizationReadAccessBRole(organizationId)
            };

            return roles.ToArray();
        }

        public string[] GetDepartmentLevelGeneralAccessRoles(string departmentId)
        {
            var roles = new List<string>
            {
                _objectArtifactPermissionGeneratorService.GeneratePoweruserRole(departmentId),
                _objectArtifactPermissionGeneratorService.GenerateManagementAccessRole(departmentId),
                _objectArtifactPermissionGeneratorService.GenerateMpaAccessRole(departmentId)
            };

            return roles.ToArray();
        }

        private string[] GetObjectArtifactDefaultRoles()
        {
            var roles = new List<string>
            {
                _objectArtifactPermissionGeneratorService.GetAdminRole(),
                _objectArtifactPermissionGeneratorService.GetTaskControllerRole()
            };

            return roles.ToArray();
        }
    }
}