using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;

public class UpdateGeneratedReportTemplateSectionCommand : PraxisGeneratedReportTemplateSection
{
    public List<PraxisGeneratedReportTemplateSection> Sections { get; set; }
    public GenerateValidationReportTemplatePdfCommand PdfGenerationPayload { get; set; }
}