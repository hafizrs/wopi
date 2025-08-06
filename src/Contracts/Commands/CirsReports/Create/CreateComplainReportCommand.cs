using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using System.Collections.Generic;

#nullable enable
namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

public class CreateComplainReportCommand : AbstractCreateCirsReportCommand
{
    public OriginatorInfo? OriginatorInfo { get; set; } = null;
    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Complain;
}

public class ReportingAttachmentFile
{
    public string ItemId { get; set; } = null!;
    public string FileStorageId { get; set; } = null!;
}