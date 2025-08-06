using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IGenerateShiftPlanReportService
    {
        bool GenerateShiftPlanReport(ExcelPackage excel, GenerateShiftPlanReportCommand command);
        void SetupRolesForShiftPlanReport(PraxisReport report, GenerateShiftPlanReportCommand command);
    }
}
