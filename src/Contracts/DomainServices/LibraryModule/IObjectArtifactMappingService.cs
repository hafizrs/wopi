using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactMappingService
    {
        Task CreateOrUpdateRiqsObjectArtifactMapping(RiqsObjectArtifactMapping mappingData, bool isUpdate);
        RiqsObjectArtifactMapping CreateRiqsObjectArtifactMappingPayload(ObjectArtifact artifact);
        Task<string> UpdateAndGetApprovalStatusFromMapping(ObjectArtifact artifact, string controlMechanismName, DateTime currentUtcTime);
    }
}