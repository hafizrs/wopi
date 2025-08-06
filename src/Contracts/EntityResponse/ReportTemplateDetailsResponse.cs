using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    [BsonIgnoreExtraElements]
    public class ReportTemplateDetailsResponse : ReportTemplatesResponse
    {
        public string HeaderText { get; set; }
        public string FooterText { get; set; }
        public PraxisImage Logo { get; set; }
        public List<ReportTemplateSectionResponse> ReportSections { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class ReportTemplateSectionResponse
    {
        [BsonElement("_id")]
        public string ItemId { get; set; }
        public string TemplateId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int SequenceNo { get; set; }
        public bool IsEnableConditionalQuestions { get; set; }
        public List<ReportTemplateSectionElement> SectionElements { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime LastUpdateDate { get; set; }
    }
}
