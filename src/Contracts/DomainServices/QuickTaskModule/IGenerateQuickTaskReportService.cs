using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;

public interface IGenerateQuickTaskReportService
{
    bool GenerateQuickTaskReport(ExcelPackage excel, GenerateQuickTaskReportCommand command);
    void SetupRolesForQuickTaskReport(PraxisReport report, GenerateQuickTaskReportCommand command);
} 