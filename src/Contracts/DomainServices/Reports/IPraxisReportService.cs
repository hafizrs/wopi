using System.Collections.Generic;
using System.Threading.Tasks;
using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IPraxisReportService
    {
        public void AddLogoInExcelReport(ExcelWorksheet workSheetTable, int logoSize, int logoPosition, string logoLocation, int columnOffsetPixel = 0);
        public void DrawBorder(ExcelWorksheet workSheetTable, int startRow, int startColumn, int endRow, int endColumn);
        public Task CreatePraxisReport(ExportReportCommand reportCommand, string moduleName);
        public Task CreatePraxisReport(GeneratePdfUsingTemplateEngineCommand reportCommand);
        Task<PraxisReport> CreatePraxisReportWithExportReportCommand(ExportReportCommand command, string moduleName);
        public Task UpdatePraxisReportStatus(string reportFileId, string status);
        Task HandlePdfGenerationEvent(PdfsFromHtmlCreatedEvent pdfFromHtmlCreatedEvent);
        Task<bool> DeletePraxisReport(string itemId);
        Task AddClientIdsToPraxisReport(string reportFileId, IEnumerable<string> clientIds);
        List<string> GetDynamicRolesForPraxisReportFromCLietIds(List<string> clientIds);
        Task InsertOrUpdatePraxisReport(PraxisReport praxisReport, string reportFileId);
        Task InsertOrUpdateRowLevelSecurity(IRowLevelSecurity roles, string reportFileId);
    }
}