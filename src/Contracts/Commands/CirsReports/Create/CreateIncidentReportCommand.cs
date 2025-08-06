using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

public class CreateIncidentReportCommand : AbstractCreateCirsReportCommand
{
    public string Topic { get; set; }
    public string Measures { get; set; }
    public bool ReportExternalOffice { get; set; }
    public bool ReportInternalOffice { get; set; }
    public List<MinimalSupplierInfo> ExternalReporters { get; set; } = new List<MinimalSupplierInfo>();

    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Incident;
}
