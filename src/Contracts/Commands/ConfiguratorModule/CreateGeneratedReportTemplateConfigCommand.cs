using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Commands.ConfiguratorModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule
{
    public class CreateGeneratedReportTemplateConfigCommand : CreateReportTemplateCommand
    {
        public string TemplateId { get; set; }
        public string GeneratedBy { get; set; }
        public string RelatedEntityName { get; set; }
        public string RelatedEntityId { get; set; }
        public string AttachedPreparationReportId { get; set; }
        public List<string> ApprovedBy { get; set; } = new List<string>();
        public IDictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
    }
}
