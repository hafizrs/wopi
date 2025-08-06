using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class ArtifactResponseEntityDictionary
    {
        public List<PraxisClient> PraxisClients { get; set; }
        public List<PraxisOrganization> PraxisOrganizations { get; set; }
        public List<RiqsLibraryControlMechanism> RiqsLibraryControlMechanisms { get; set; }
        public List<DocumentEditMappingRecord> DocumentEditMappingRecords { get; set; }
        public List<RiqsObjectArtifactMapping> RiqsObjectArtifactMappings { get; set; }
        public List<PraxisUser> PraxisUsers { get; set; }
        public List<DmsArtifactUsageReference> DmsArtifactUsageReferences { get; set; }
        public RiqsPediaViewControlResponse RiqsViewControl { get; set; }
        public string LoggedInDepartmentId { get; set; }
        public string LoggedInOrganizationId { get; set; }
    }

}