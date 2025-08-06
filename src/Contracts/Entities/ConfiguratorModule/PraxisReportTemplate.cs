using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule
{
    [BsonIgnoreExtraElements]
    public class PraxisReportTemplate : EntityBase
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<ClientInfo> ClientInfos { get; set; }
        public List<OrganizationInfo> OrganizationInfos { get; set; }
        public string HeaderText { get; set; }
        public string FooterText { get; set; }
        public PraxisImage Logo { get; set; }
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public bool IsAPreparationReport { get; set; }
    }

    public class ClientInfo
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
    }

    public class OrganizationInfo
    {
        public string OrganizationId { get; set; }
        public string OrganizationName { get; set; }
    }

    public enum ReportStatus
    {
        Pending = 0,
        Drafted = 1,
        Generated = 2,
        Published = 3,
        PartiallyApproved = 4,
        Approved = 5,
        Signed = 6
    }
}
