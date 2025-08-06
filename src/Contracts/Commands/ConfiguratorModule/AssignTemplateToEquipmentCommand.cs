using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule
{
    public class AssignTemplateToEquipmentCommand
    {
        public string EquipmentId { get; set; }
        public List<EquipmentReportTemplate> EquipmentReportTemplates { get; set; }
    }

    public class EquipmentReportTemplate
    {
        public string TemplateId { get; set; }
        public string TemplateType { get; set; }
    }
}
