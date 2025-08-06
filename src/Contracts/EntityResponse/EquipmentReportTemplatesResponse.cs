using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    [BsonIgnoreExtraElements]
    public class EquipmentReportTemplatesResponse
    {
        [BsonElement("_id", Order = 0)]
        public string ItemId { get; set; }
        public string Title { get; set; }
        public string EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public string ReportType { get; set; }
        public string Description { get; set; }
        public List<ClientInfo> ClientInfos { get; set; }
        public List<OrganizationInfo> OrganizationInfos { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreatedBy { get; set; }
        public bool IsAPreparationReport { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public ReportStatus NextStatus { get; set; }
        public string RoomName { get; set; }
        public string SerialNumber { get; set; }
        public string InstallationNumber { get; set; }
        public string UdiNumber { get; set; }
        public string InternalNumber { get; set; }
    }
}
