using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

#nullable enable
public abstract class AbstractCreateCirsReportCommand
{
    public string CirsReportId { get; set; } = null!;
    public string OrganizationId { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public IEnumerable<string> Tags { get; set; } = null!;
    public string Title { get; set; } = null!;
    public IEnumerable<string> KeyWords { get; set; } = null!;
    public string? Description { get; set; }
    public IEnumerable<string>? AttachmentIds { get; set; }
    public string? Remarks { get; set; }
    public ReportingVisibility? ReportingVisibility { get; set; }
    public IEnumerable<AffectedInvolvedParty> AffectedInvolvedParties { get; set; } = null!;
    public List<ReportingAttachmentFile>? AttachedDocuments { get; set; } = null;
    public ReportingAttachmentFile? AttachedForm { get; set; } = null;
    public IDictionary<string, string>? MetaData { get; set; } = null;
    public List<CirsEditHistory>? CirsEditHistory { get; set; }
    abstract public CirsDashboardName CirsDashboardName { get; }
}
