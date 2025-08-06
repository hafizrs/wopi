using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactAuthorizationCheckerService
    {
        bool IsAAdminOrTaskConrtroller();
        bool IsAMpaUser();
        bool CanApproveObjectArtifact(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null, RiqsPediaViewControlResponse viewControl = null);
        bool CanActiveInactiveObjectArtifact(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null, List<DmsArtifactUsageReference> usageReferences = null, RiqsPediaViewControlResponse viewControl = null);
        bool IsAReapprovedArtifact(IDictionary<string, MetaValuePair> metaData, bool checkReapproveProcess);
        bool IsReapproveProcessStarted(IDictionary<string, MetaValuePair> metaData);
        bool IsALibraryApprovalAdmin(string organizationId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null);
        bool IsALibraryUploadAdmin(string organizationId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null);
        bool? IsAFolderLevelLibraryApprovalAdmin(string parentId, List<RiqsObjectArtifactMapping> artifactMappingDatas = null);
        bool IsAAdminBUser();
        bool IsAPowerUser();
        bool IsAManagementUser();
        bool IsAEditAllowedUser(RiqsObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null, RiqsPediaViewControlResponse viewControl = null);
        bool IsAShareAllowedUser(RiqsObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null, RiqsPediaViewControlResponse viewControl = null);
        bool IsAArtifactUploadRestrictedUser(RiqsObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null);
        bool IsAFormFillRestrictedUser(RiqsObjectArtifact artifact);
        bool CanMoveObjectArtifact(RiqsObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null, RiqsPediaViewControlResponse viewControl = null);
        bool HaveNextReapproveDateKey(IDictionary<string, MetaValuePair> metaData);
        bool IsALibraryAuthorityMember(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null);
        bool IsArtifactBeingUsed(string objectArtifactId);
        bool CanWriteObjectArtifact(ObjectArtifact artifact);
        bool IsALibraryDeptLevelAdmin(List<RiqsLibraryControlMechanism> controlMechanismDatas = null);
        bool HaveDeptLevelAdminPermission(ObjectArtifact artifact, List<RiqsLibraryControlMechanism> controlMechanismDatas = null);
    }
}
