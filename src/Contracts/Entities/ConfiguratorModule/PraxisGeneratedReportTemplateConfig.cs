using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule
{
    public class PraxisGeneratedReportTemplateConfig : PraxisReportTemplate
    {
        public string TemplateId { get; set; }
        public string GeneratedBy { get; set; }
        public string RelatedEntityName { get; set; }
        public string RelatedEntityId { get; set; }
        public string AttachedPreparationReportId { get; set; }
        public List<string> ApprovedBy { get; set; } = new List<string>();
        public IDictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public ReportStatus NextStatus { get; set; }
    }
}
