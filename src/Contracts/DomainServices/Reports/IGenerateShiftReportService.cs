using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;

public interface IGenerateShiftReportService
{
    bool GenerateShiftReport(ExcelPackage excel, GenerateShiftReportCommand command);
    void SetupRolesForShiftReport(PraxisReport report, GenerateShiftReportCommand command);
}