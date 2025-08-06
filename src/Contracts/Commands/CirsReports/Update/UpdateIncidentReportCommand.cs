using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;

#nullable enable
public class UpdateIncidentReportCommand : AbstractUpdateCirsReportCommand
{
    public string? Topic { get; set; }

    public string? Measures { get; set; }
    public bool? ReportExternalOffice { get; set; }
    public bool? ReportInternalOffice { get; set; }
    public List<MinimalSupplierInfo> ExternalReporters { get; set; } = new List<MinimalSupplierInfo>();

    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Incident;
}
