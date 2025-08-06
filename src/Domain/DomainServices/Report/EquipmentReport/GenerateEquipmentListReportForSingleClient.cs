using System;
using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.EquipmentReport;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.EquipmentReport;

public class GenerateEquipmentListReportForSingleClient : IGenerateEquipmentListReport
{
    private readonly ILogger<GenerateEquipmentListReportForSingleClient> _logger;
    private readonly IRepository _repository;
    private readonly IGenerateEquipmentReport _generateEquipmentReportService;

    public GenerateEquipmentListReportForSingleClient(
        ILogger<GenerateEquipmentListReportForSingleClient> logger,
        IRepository repository,
        IGenerateEquipmentReport generateEquipmentReportService)
    {
        _logger = logger;
        _repository = repository;
        _generateEquipmentReportService = generateEquipmentReportService;
    }

    public async Task<bool> GenerateReport(ExcelPackage excel, ExportEquipmentListReportCommand command)
    {
        _logger.LogInformation("Entered into Service: {ServiceName}", nameof(GenerateEquipmentListReportForSingleClient));
        try
        {
            var client = _repository.GetItem<PraxisClient>(
                pc => pc.ItemId.Equals(command.ClientId) && !pc.IsMarkedToDelete
            );

            if (client == null)
            {
                return false;
            }

            var isReportPrepared = await _generateEquipmentReportService.PrepareEquipmentListReport(
                command.FilterString,
                command.EnableDateRange,
                client,
                excel,
                command.Translation
            );
            return isReportPrepared;
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in Service: {ServiceName}. Error Message: {Message}. Details: {StackTrace}.", 
                nameof(GenerateEquipmentListReportForSingleClient), e.Message, e.StackTrace);
            return false;
        }
    }
}