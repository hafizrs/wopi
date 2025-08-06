using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    [BsonIgnoreExtraElements]
    public class ReportTemplatesResponse
    {
        [BsonElement("_id", Order = 0)]
        public string ItemId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<ClientInfo> ClientInfos { get; set; }
        public List<OrganizationInfo> OrganizationInfos { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreatedBy { get; set; }
        public bool IsAPreparationReport { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime LastUpdateDate { get; set; }
    }
}
