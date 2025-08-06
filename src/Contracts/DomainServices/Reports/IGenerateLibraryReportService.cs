using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IGenerateLibraryReportService
    {
        bool GenerateLibraryReport(ExcelPackage excel, GenerateLibraryReportCommand command);
        void SetupRolesForLibraryReport(PraxisReport report, GenerateLibraryReportCommand command);
        bool GenerateLibraryFolderStructureReport(ExcelPackage excel, GenerateLibraryReportCommand command);
    }
}
