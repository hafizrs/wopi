using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactSharedDataResponseGeneratorService
    {
        SharedObjectArtifactResponse GetSharedObjectArtifactResponse(ObjectArtifact objectArtifact, List<PraxisUser> praxisUsers = null, List<PraxisClient> praxisClients = null, List<PraxisOrganization> praxisOrganizations = null, RiqsPediaViewControlResponse riqsViewControl = null);
        AssigneeDetail GetObjectArtifactAssigneeDetailResponse(ObjectArtifact objectArtifact, List<PraxisUser> praxisUsers = null, List<PraxisOrganization> praxisOrganizations = null, RiqsPediaViewControlResponse riqsViewControl = null);
        (AssignedMemberDetail, AssignedMemberDetail) GetFormFillActionDetails(ObjectArtifact objectArtifact, List<PraxisUser> praxisUsers = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null);
        (AssignedMemberDetail, AssignedMemberDetail) GetFormFillActionDetailsForFilledForm(ObjectArtifact objectArtifact, List<PraxisUser> praxisUsers = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null);
    }
}
