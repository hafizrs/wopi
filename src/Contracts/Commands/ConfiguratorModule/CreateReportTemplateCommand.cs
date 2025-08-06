using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Commands.ConfiguratorModule
{
    public class CreateReportTemplateCommand
    {
        public string ItemId { get; set; }
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
}
