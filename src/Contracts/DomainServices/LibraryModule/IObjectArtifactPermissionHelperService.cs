using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactPermissionHelperService
    {
        bool IsAAdminBUpload(string userId, string organizationId);
        bool IsALibraryAdminUpload(ObjectArtifact artifact, string userId);
        bool IsAPowerUserUpload(string userId, string departmentId);
        string[] GetOrganizationLevelObjectArtifactRoles(string organizationId);
        string[] GetDepartmentLevelObjectArtifactRoles(string organizationId, string departmentId);
        string[] GetOrganizationLevelObjectArtifactFileApproverRoles(string organizationId);
        string[] GetDepartmentLevelObjectArtifactFileApproverRoles(string organizationId, string departmentId);
        string[] GetOrganizationLevelObjectArtifactRemoverRoles(string organizationId);
        string[] GetDepartmentLevelObjectArtifactRemoverRoles(string organizationId, PraxisClient department);
        string[] GetLibraryAllAdminUserIds(string organizationId, string parentId);
        string[] GetObjectArtifactAuthorizedIds(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, bool onlyDeptLevel = false);
        string[] GetOrganizationLevelGeneralAccessRoles(string organizationId);
        string[] GetDepartmentLevelGeneralAccessRoles(string departmentId);
    }
}