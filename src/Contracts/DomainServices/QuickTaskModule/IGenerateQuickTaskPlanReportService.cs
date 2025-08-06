using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;

public interface IGenerateQuickTaskPlanReportService
{
    bool GenerateQuickTaskPlanReport(ExcelPackage excel, GenerateQuickTaskPlanReportCommand command);
    void SetupRolesForQuickTaskPlanReport(PraxisReport report, GenerateQuickTaskPlanReportCommand command);
} 