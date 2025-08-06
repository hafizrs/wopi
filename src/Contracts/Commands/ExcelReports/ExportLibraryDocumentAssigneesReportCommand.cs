using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

public class ExportLibraryDocumentAssigneesReportCommand : ExportReportCommand
{
    public string ObjectArtifactId { get; set; }
    public LibraryAssignedMemberType Purpose { get; set; }
    public List<string> ClientIds { get; set; }
}