using System;
using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

#nullable enable
namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

public class CreateHintReportCommand : AbstractCreateCirsReportCommand
{
    public bool ReportInternalOffice { get; set; }

    public bool ReportExternalOffice { get; set; }

    public List<MinimalSupplierInfo> ExternalReporters { get; set; } = new List<MinimalSupplierInfo>();

    public string? ReporterClientId { get; set; }

    public DateTime ReportingDate { get; set; }
    public InvolvedUser? ReportedBy{ get; set; }

    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Hint;
}
