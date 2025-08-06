using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule
{
    public interface ICockpitDocumentActivityMetricsGenerationService
    {
        Task OnDocumentUploadGenerateActivityMetrics(string objectArtifactId);
        Task OnDocumentApproveGenerateActivityMetrics(string objectArtifactId);
        Task OnDocumentShareGenerateActivityMetrics(string[] objectArtifactIds, string activityName);
        Task OnDocFileEditGenerateActivityMetrics(string[] parentObjectArtifactIds);
        Task OnDocFileSaveGenerateActivityMetrics(string parentObjectArtifactId);
        Task OnFormFillGenerateActivityMetrics(string[] originalFormIds, string shiftPlanId = null, bool isCompleted = false);
        Task OnDocumentDeleteGenerateActivityMetrics(string[] objectArtifactIds);
        Task OnDocumentRenameGenerateObjectArtifactSummary(string objectArtifactId);
        Task OnDocumentMoveGenerateObjectArtifactSummary(string[] objectArtifactIds);
        Task OnOrganizationLibraryRightsUpdateGenerateActivityMetrics(string organizationId);
        Task OnDocumentReapproveGenerateObjectArtifactSummary(string[] objectArtifactIds);
        Task OnDocumentMarkAsReadActivityMetrics(string objectArtifactId);
        Task OnDocumentActivateDeactivateUpdateSummary(string objectArtifactId, bool activate);
        Task OnDocumentUsedInShiftPlanGenerateActivityMetrics(string[] objectArtifactIds, string activityName, string shiftPlanId);
        Task OnDeletingShiftPlanDeleteFormsSummary(string[] objectArtifactIds, string activityName, RiqsShiftPlan shiftPlan);
        Task MarkedToDeleteCockpitDocumentSummaryByShiftPlan(List<string> objectArtifactIds, string shiftPlanId);
        Task<DeleteResult> DeleteCockpitObjectArtifactSummary(string[] summaryIds);
    }
}