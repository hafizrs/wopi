using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.ProcessGuideReport.DeveloperReport;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.ProcessGuideReport.DeveloperReport
{
    public class GenerateDeveloperProcessGuideReport : IGenerateDeveloperProcessGuideReport
    {
        private readonly ILogger<GenerateDeveloperProcessGuideReport> _logger;
        private readonly IProcessGuideReportGenerateStrategy _processGuideReportGenerateStrategy;
        private readonly IStorageDataService _storageDataService;

        public GenerateDeveloperProcessGuideReport(
            ILogger<GenerateDeveloperProcessGuideReport> logger,
            IProcessGuideReportGenerateStrategy processGuideReportGenerateStrategy,
            IStorageDataService storageDataService)
        {
            _logger = logger;
            _processGuideReportGenerateStrategy = processGuideReportGenerateStrategy;
            _storageDataService = storageDataService;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<bool> GenerateReport(ExportProcessGuideReportForDeveloperCommand command)
        {
            try
            {
                using ExcelPackage excel = new ExcelPackage();
                var service = _processGuideReportGenerateStrategy.GetReportType(command.IsReportForAllData);
                var isReportPrepared = await service.GenerateReport(excel, command);
                if (isReportPrepared)
                {
                    var isSuccess = await _storageDataService.UploadFileAsync(command.ReportFileId.ToString(), command.FileNameWithExtension,
                        excel.GetAsByteArray());

                    _logger.LogInformation("Process guide developer report uploaded to storage success -> {IsSuccess}", isSuccess);

                    return isSuccess;
                }
                return false;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during generate Process guide overview report. Exception message: {Message}. Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }
    }
}
