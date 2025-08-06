using System.Collections.Generic;
using System.Threading.Tasks;
using OfficeOpenXml;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;

public interface IGenerateLibraryDocumentAssigneesReportService
{
    Task<bool> GenerateLibraryDocumentAssigneesReportAsync(ExcelPackage excel, ExportLibraryDocumentAssigneesReportCommand command);
    IRowLevelSecurity PrepareRowLevelSecurity(List<string> clientIds);
    Task UpdateClientsInReport(string reportFileId, List<string> clientIds);
}