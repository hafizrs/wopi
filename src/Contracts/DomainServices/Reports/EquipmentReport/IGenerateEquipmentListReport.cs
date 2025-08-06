using System.Threading.Tasks;
using OfficeOpenXml;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.EquipmentReport;

public interface IGenerateEquipmentListReport
{
    Task<bool> GenerateReport(ExcelPackage excel, ExportEquipmentListReportCommand command);
}