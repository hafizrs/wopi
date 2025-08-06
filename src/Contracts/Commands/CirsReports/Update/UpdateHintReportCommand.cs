using System;
using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

#nullable enable
namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;

public class UpdateHintReportCommand : AbstractUpdateCirsReportCommand
{
    public bool? ReportInternalOffice { get; set; }

    public bool? ReportExternalOffice { get; set; }

    public string? ReporterClientId { get; set; }

    public DateTime? ReportingDate { get; set; }
    public string? DecisionSelection { get; set; }
    public string? DecisionSelectionReason { get; set; }
    public List<MinimalSupplierInfo> ExternalReporters { get; set; } = new List<MinimalSupplierInfo>();
    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Hint;
}
