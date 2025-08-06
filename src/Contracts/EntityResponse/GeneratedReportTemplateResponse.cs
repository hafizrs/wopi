using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    [BsonIgnoreExtraElements]
    public class GeneratedReportTemplateResponse
    {
        [BsonElement("_id")]
        public string ItemId { get; set; }
        public string TemplateId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreateDate { get; set; }
        public ReportStatus Status { get; set; }
        public string GeneratedBy { get; set; }
        public List<ClientInfo> ClientInfos { get; set; }
        public List<OrganizationInfo> OrganizationInfos { get; set; }
        public bool IsAPreparationReport { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public string RelatedEntityId { get; set; }
        public bool CanApproveReport { get; set; }
        public List<string> ApprovedBy { get; set; } = new List<string>();
        public string AttachedPreparationReportId { get; set; }
        public ReportStatus NextStatus { get; set; }
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
    }

    [BsonIgnoreExtraElements]
    public class GeneratedReportTemplateDetailsResponse : GeneratedReportTemplateResponse
    {
        public string HeaderText { get; set; }
        public string FooterText { get; set; }
        public PraxisImage Logo { get; set; }
        [BsonElement(Order = 1000)]
        public List<GeneratedReportTemplateSectionResponse> ReportSections { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class GeneratedReportTemplateSectionResponse
    {
        [BsonElement("_id")]
        public string ItemId { get; set; }
        public string TemplateId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int SequenceNo { get; set; }
        public bool IsEnableConditionalQuestions { get; set; }
        [BsonElement("GeneratedSectionElements")]
        public List<GeneratedReportTemplateSectionElement> SectionElements { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime LastUpdateDate { get; set; }
    }
}
