using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;

#nullable enable
public abstract class AbstractUpdateCirsReportCommand
{
    public string CirsReportId { get; set; } = null!;
    public string? Title { get; set; }
    public IEnumerable<string>? KeyWords { get; set; }
    public string? Description { get; set; }
    public IEnumerable<string>? AttachmentIds { get; set; }
    public string? Remarks { get; set; }
    public string? Status { get; set; }
    public IEnumerable<string> Tags { get; set; } = null!;
    public RankDetails? RankDetails { get; set; }
    public IEnumerable<AffectedInvolvedParty>? AffectedInvolvedParties { get; set; } = null!;
    public IEnumerable<InvolvedUser>? ResponsibleUsers { get; set; }
    public List<ReportingAttachmentFile>? AttachedDocuments { get; set; } = null;
    public ReportingAttachmentFile? AttachedForm { get; set; } = null;
    public string ClientId { get; set; } = null!;
    public ReportingVisibility? ReportingVisibility { get; set; }
    public IDictionary<string, string>? MetaData { get; set; } = null;
    public bool? IsDuplicate { get; set; }
    public List<CirsEditHistory>? CirsEditHistory { get; set; }
    abstract public CirsDashboardName CirsDashboardName { get; }
}

public class RankDetails
{
    public string? RankAfterId { get; set; }
    public string? RankBeforeId { get; set; }
}
