using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IExportReportService
    {
        bool Export(Guid fileId, string fileNameWithExtension, string filter);

        Task<bool> ExportTaskListReport(ExportTaskListReportCommand command);

        Task<bool> ExportDistinctTaskListReport(ExportDistinctTaskListReportCommand command);

        Task<bool> ExportOpenItemReport(ExportOpenItemReportCommand command);

        Task<bool> ExportEquipmentListReport(ExportEquipmentListReportCommand command);

        Task<bool> ExportEquipmentMaintenanceListReport(ExportEquipmentMaintenanceListReportCommand command);

        Task<bool> ExportCategoryReport(ExportCategoryReportCommand command);

        Task<bool> ExportTrainingReport(ExportTrainingReportCommand command);
        Task<bool> ExportTrainingDetailsReport(ExportTrainingDetailsReportCommand command);
        Task<bool> ExportDeveloperReport(ExportDeveloperReportCommand command);
        Task<bool> ExportPraxisUserListReport(ExportPraxisUserListReportCommand command);
        Task<bool> ExportRiskOverviewReport(ExportRiskOverviewReportCommand command);
        Task<bool> ExportProcessGuideDeveloperReport(ExportProcessGuideReportForDeveloperCommand command);
        Task<bool> ExportPhotoDocumentationReport(string htmlFileId, string reportFileId, string reportFileName);
        Task<bool> ExportCirsReport(ExportReportCommand command);
        Task<bool> GenerateShiftPlanReportAsync(GenerateShiftPlanReportCommand command);
        Task<bool> GenerateShiftReportAsync(GenerateShiftReportCommand command);
        Task<bool> GenerateLibraryReportAsync(GenerateLibraryReportCommand command);
        Task<bool> ExportSuppliersReportAsync(ExportSuppliersReportCommand command);
        Task<bool> ExportLibraryDocumentAssigneesReportAsync(ExportLibraryDocumentAssigneesReportCommand command);
        Task<bool> GenerateQuickTaskPlanReportAsync(GenerateQuickTaskPlanReportCommand command);
        Task<bool> GenerateQuickTaskReportAsync(GenerateQuickTaskReportCommand command);
    }
}
