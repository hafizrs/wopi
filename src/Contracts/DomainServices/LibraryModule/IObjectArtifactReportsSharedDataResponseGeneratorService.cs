using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactReportsSharedDataResponseGeneratorService
    {

        LibraryReportAssigneeDetail GetObjectArtifactAssigneeDetailResponse(
            string organizationId,
            IDictionary<string, MetaValuePair> metaData,
            List<SharedOrganizationInfo> sharedOrganizationList);
    }
}