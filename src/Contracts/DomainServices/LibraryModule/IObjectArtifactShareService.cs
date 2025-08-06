using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactShareService
    {
        Task<bool> ShareObjectArtifact(ObjectArtifact objectArtifact, ObjectArtifactFileShareCommand accessControlCommand);
        ObjectArtifactFileShareCommand GetCurrentAccessControl(ObjectArtifact objectArtifact);
        bool IsObjectArtifactInASharedDirectory(ObjectArtifact objectArtifact);
        Task<bool> InitiateShareWithParentSharedUsers(ObjectArtifact objectArtifact);
        Task<bool> ShareGeneralForm(ObjectArtifact objectArtifact);
        IDictionary<string, MetaValuePair> PrepareObjectArtifactMetaDataUpdate(IDictionary<string, MetaValuePair> metaData, DateTime currentTime, bool? isStandardFile = null, bool isNotifiedToCockpit = false);
        string[] GetSharedIdsAllowedToRead(List<SharedOrganizationInfo> sharedOrganizationList);
        string[] GetSharedIdsAllowedToUpdate(List<SharedOrganizationInfo> sharedOrganizationList);
    }
}