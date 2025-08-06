using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule
{
    public class CreateGeneratedReportTemplateSectionCommand
    {
        public List<PraxisGeneratedReportTemplateSection> Sections { get; set; }
        public GenerateValidationReportTemplatePdfCommand PdfGenerationPayload { get; set; }
    }
}
