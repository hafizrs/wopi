using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;

public class UpdateGeneratedReportTemplateConfigCommand
{
    public string ItemId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string HeaderText { get; set; }
    public string FooterText { get; set; }
    public PraxisImage Logo { get; set; }
    public ReportStatus Status { get; set; }
    public bool IsAPreparationReport { get; set; }
    public string AttachedPreparationReportId { get; set; }
    public List<string> ApprovedBy { get; set; } = new List<string>();
    public IDictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
}