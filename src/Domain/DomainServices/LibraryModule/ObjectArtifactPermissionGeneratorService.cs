using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class ObjectArtifactPermissionGeneratorService : IObjectArtifactPermissionGeneratorService
    {
        private readonly IRepository _repository;

        public ObjectArtifactPermissionGeneratorService(IRepository repository)
        {
            _repository = repository;
        }

        public string GetAdminRole()
        {
            return $"{RoleNames.Admin}";
        }

        public string GetTaskControllerRole()
        {
            return  $"{RoleNames.TaskController}";
        }

        public string GenerateAdminBRole(string organizationId)
        {
            return $"{RoleNames.AdminB_Dynamic}_{organizationId}";
        }

        public string GenerateOrganizationReadAccessBRole(string organizationId)
        {
            return  $"{RoleNames.Organization_Read_Dynamic}_{organizationId}";
        }

        public List<string> GetLibraryApprovalAdminUserIds(string organizationId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null)
        {
            var ids = new List<string>();
            if (!string.IsNullOrWhiteSpace(organizationId))
            {
                var controlMechanismData = LibraryControlMechanismConstant.GetLibraryControlMechanismDataByOrgId(organizationId, controlMechanismDatas);
                if (controlMechanismData?.ApprovalAdmins?.Count > 0)
                {
                    ids = controlMechanismData?.ApprovalAdmins?.Select(admin => admin.UserId)?.ToList();
                }
            }

            return ids;
        }

        public List<string> GetLibraryUploadAdminUserIds(string organizationId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null)
        {
            var ids = new List<string>();
            if (!string.IsNullOrWhiteSpace(organizationId))
            {
                var controlMechanismData = LibraryControlMechanismConstant.GetLibraryControlMechanismDataByOrgId(organizationId, controlMechanismDatas);
                if (controlMechanismData?.UploadAdmins?.Count > 0)
                {
                    ids = controlMechanismData?.UploadAdmins?.Select(admin => admin.UserId)?.ToList();
                }
            }

            return ids;
        }

        public List<string> GetLibraryDeptLevelAdminUserIds(string deptId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null)
        {
            var ids = new List<string>();
            if (!string.IsNullOrWhiteSpace(deptId))
            {
                var controlMechanismData = LibraryControlMechanismConstant.GetLibraryControlMechanismDataByDeptId(deptId, controlMechanismDatas);
                if (controlMechanismData?.ApprovalAdmins?.Count > 0)
                {
                    ids = controlMechanismData?.ApprovalAdmins?.Select(admin => admin.UserId)?.ToList() ?? new List<string>();
                }
                if (controlMechanismData?.UploadAdmins?.Count > 0)
                {
                    ids.AddRange(controlMechanismData?.UploadAdmins?.Select(admin => admin.UserId)?.ToList() ?? new List<string>());
                }
            }

            return ids.Distinct().ToList();
        }

        public List<string> GetLibraryApprovalAdminUserIdsByParentFolderId(string artifactId, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            var ids = new List<string>();
            if (!string.IsNullOrWhiteSpace(artifactId))
            {
                var mappingData = RiqsObjectArtifactMappingConstant.GetRiqsObjectArtifactMappingByParentFolderId(artifactId, artifactMappingDatas);
                if (mappingData?.ApprovalAdmins?.Count > 0)
                {
                    ids = mappingData?.ApprovalAdmins?.Select(admin => admin.UserId)?.ToList();
                }
            }

            return ids;
        }

        public string GeneratePoweruserRole(string departmentId)
        {
            return $"{RoleNames.PowerUser_Dynamic}_{departmentId}";
        }

        public string GenerateManagementAccessRole(string departmentId)
        {
            return $"{RoleNames.Leitung_Dynamic}_{departmentId}";
        }

        public string GenerateMpaAccessRole(string departmentId)
        {
            return $"{RoleNames.MpaGroup_Dynamic}_{departmentId}";
        }
    }
}
