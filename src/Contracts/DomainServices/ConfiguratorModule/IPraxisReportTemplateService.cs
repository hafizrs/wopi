using Selise.Ecap.SC.PraxisMonitor.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.ConfiguratorModule;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule
{
    public interface IPraxisReportTemplateService
    {
        Task CreateReportTemplate(CreateReportTemplateCommand command);
        Task CreateReportTemplateSection(CreateReportTemplateSectionCommand command);

        Task CreateGeneratedReportTemplateConfig(CreateGeneratedReportTemplateConfigCommand command);
        Task CreateGeneratedReportTemplateSection(CreateGeneratedReportTemplateSectionCommand command);

        Task UpdateReportTemplate(UpdateReportTemplateCommand command);
        Task UpdateReportTemplateSection(UpdateReportTemplateSectionCommand command);
        Task UpdateGeneratedReportTemplateConfig(UpdateGeneratedReportTemplateConfigCommand command);
        Task UpdateGeneratedReportTemplateSection(UpdateGeneratedReportTemplateSectionCommand command);


        Task<EntityQueryResponse<ReportTemplatesResponse>> GetReportTemplates(GetReportTemplatesQuery query);
        Task<EntityQueryResponse<ReportTemplateDetailsResponse>> GetReportTemplateDetails(GetReportTemplateDetailsQuery query);
        Task<EntityQueryResponse<ReportTemplateSectionResponse>> GetReportTemplateSections(GetReportTemplateSectionsQuery query);

        Task<EntityQueryResponse<GeneratedReportTemplateResponse>> GetGeneratedReportTemplates(GetGeneratedReportTemplateQuery query);
        Task<EntityQueryResponse<GeneratedReportTemplateDetailsResponse>> GetGeneratedReportTemplateDetails(GetGeneratedReportTemplateDetailsQuery query);
        Task<EntityQueryResponse<GeneratedReportTemplateSectionResponse>> GetGeneratedReportTemplateSections(GetGeneratedReportTemplateSectionsQuery query);

        Task DeleteReportTemplate(DeleteReportTemplateCommand command);
        Task DeleteGeneratedReportTemplateConfig(DeleteGeneratedReportTemplateConfigCommand command);

        Task AssignTemplateToEquipment(AssignTemplateToEquipmentCommand command);
        Task<QueryHandlerResponse> GetPastReportSummary(GetPastReportSummariesQuery query);


        Task GenerateHtmlForPdfAsync(GenerateValidationReportTemplatePdfCommand command, bool isSignaturePending = false);
        Task PrepareHtmlCommandAndGeneratePdf(string reportId, bool isSignaturePending = false);
        Task UpdateReportTemplateConfigWithPdfFileId(string reportItemId, string pdfOutputFileId);

        Task<bool> ApproveGeneratedReportByLoggedInUser(ApproveGeneratedReportCommand command);
        Task<EntityQueryResponse<EquipmentReportTemplatesResponse>> GetEquipmentReportTemplates(GetEquipmentReportTemplatesQuery query);
        Task OnMaintenanceUpdateAdjustReportStatus(string maintenanceId);
        Task UpdateExternalSignatureSignedStatus(PraxisGeneratedReportTemplateConfig reportConfig);
        Task OnMaintenanceDeletedRemoveGeneratedReports(List<string> maintenanceIds);
    }
}
