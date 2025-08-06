using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactPermissionGeneratorService
    {
        string GetAdminRole();
        string GetTaskControllerRole();
        string GenerateAdminBRole(string organizationId);
        string GenerateOrganizationReadAccessBRole(string organizationId);
        string GeneratePoweruserRole(string departmentId);
        string GenerateManagementAccessRole(string departmentId);
        string GenerateMpaAccessRole(string departmentId);
        List<string> GetLibraryApprovalAdminUserIds(string organizationId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null);
        List<string> GetLibraryUploadAdminUserIds(string organizationId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null);
        List<string> GetLibraryDeptLevelAdminUserIds(string deptId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null);
        List<string> GetLibraryApprovalAdminUserIdsByParentFolderId(string artifactId, List<RiqsObjectArtifactMapping> artifactMappingDatas = null);
    }
}
