using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SWICA;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

public interface IReportingTaskCockpitSummaryCommandService
{
    Task OnProcessGuideAdditionUpdateSummary(string processGuideId);

    Task OnProcessGuideDeletionUpdateSummary(string processGuideId);
    Task OnOpenItemAdditionUpdateSummary(string openItemId);
    Task OnOpenItemDeletionUpdateSummary(string openItemId);
    Task OnProcessGuideCompletedUpdateSummary(PraxisProcessGuideAnswer answer, PraxisProcessGuide processGuide);
    Task OnOpenItemCompletedUpdateSummary(PraxisOpenItemCompletionInfo answer, PraxisOpenItem openItem);
    Task UpdateSummaryWithAssignedAdmins(RiqsTaskCockpitSummary summary, List<PraxisIdDto> assignedAdmins);
    void OnDependentTaskModifyUpdateSummary(RiqsTaskCockpitSummary summary);
    Task UpdateSummaryOnChangingDashboard(string cirsReportId, CirsDashboardName newDashboardNameEnum);
}