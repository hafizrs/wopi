using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ProcessOrganizationExternalOfficeCommand
    {
        public string OrganizationId { get; set; }
        public string DeleteReportingId { get; set; }
        public ExternelReportingOffice ExternelReportingOffice { get; set; }
    }
}