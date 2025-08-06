using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class UserGroupingModel
    {
        public string RelatedEntityName { get; set; }
        public string RelatedEntityId { get; set; }
        public List<PraxisUser> PraxisUsers { get; set; }
    }

    public class CockpitDocumentUserMappingModel
    {
        public ObjectArtifact ObjectArtifact { get; set; }
        public CockpitObjectArtifactSummary CockpitObjectArtifactSummary { get; set; }
        public List<UserGroupingModel> GroupedPraxisUsers { get; set; }
    }

    public class CockpitClientUserMapping
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public List<PraxisUser> PraxisUsers { get; set; }
        public bool IsAllUserSelected { get; set; }
        public string OrganizationId { get; set; }
    }

}