using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.EquipmentReport;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.EquipmentReport;

public class GenerateEquipmentListReportForMultipleClient : IGenerateEquipmentListReport
{
    private readonly ILogger<GenerateEquipmentListReportForMultipleClient> _logger;
    private readonly IRepository _repository;
    private readonly IGenerateEquipmentReport _generateEquipmentReportService;
    private readonly IPraxisReportService _praxisReportService;
    private readonly ISecurityHelperService _securityHelperService;

    public GenerateEquipmentListReportForMultipleClient(
        ILogger<GenerateEquipmentListReportForMultipleClient> logger,
        IRepository repository,
        IGenerateEquipmentReport generateEquipmentReportService,
        IPraxisReportService praxisReportService,
        ISecurityHelperService securityHelperService)
    {
        _logger = logger;
        _repository = repository;
        _generateEquipmentReportService = generateEquipmentReportService;
        _praxisReportService = praxisReportService;
        _securityHelperService = securityHelperService;
    }

    public async Task<bool> GenerateReport(ExcelPackage excel, ExportEquipmentListReportCommand command)
    {
        _logger.LogInformation("Entered into Service: {ServiceName}",
            nameof(GenerateEquipmentListReportForMultipleClient));
        try
        {
            var isPrepared = await _generateEquipmentReportService.PrepareEquipmentListReport(
                command.FilterString,
                command.EnableDateRange,
                null,
                excel,
                command.Translation
            );
            var rowLevelSecurity = PrepareRowLevelSecurity(command);
            await _praxisReportService.InsertOrUpdateRowLevelSecurity(rowLevelSecurity, command.ReportFileId);
            await UpdateClientInReport(command.ReportFileId, command.OrganizationId);
            return isPrepared;
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in Service: {ServiceName} Error Message: {Message}. Details: {StackTrace}",
                nameof(GenerateEquipmentListReportForMultipleClient), e.Message, e.StackTrace);
            return false;
        }
    }
    private List<string> GetClientIds(string orgId)
    {
        if (string.IsNullOrEmpty(orgId))
        {
            return new List<string>();
        }
        var clientIds = _repository
            .GetItems<PraxisClient>(p => p.ParentOrganizationId == orgId && !p.IsMarkedToDelete)?
            .Select(x => x.ItemId)
            .ToList() ?? new List<string>();
        return clientIds;
    }

    private IRowLevelSecurity PrepareRowLevelSecurity(ExportEquipmentListReportCommand command)
    {
        var clientIds = GetClientIds(command.OrganizationId);
        IRowLevelSecurity rowLevelSecurity = new PraxisReport();
        if (!_securityHelperService.IsAAdminOrTaskConrtroller())
        {
            var allowedRoles = _praxisReportService.GetDynamicRolesForPraxisReportFromCLietIds(clientIds).ToArray();
            rowLevelSecurity.RolesAllowedToRead = allowedRoles;
            rowLevelSecurity.RolesAllowedToUpdate = allowedRoles;
        }
        rowLevelSecurity.IdsAllowedToRead = _generateEquipmentReportService.GetEquipmentAssignedOrgAdmin(command.OrganizationId).ToArray();
        rowLevelSecurity.IdsAllowedToUpdate = rowLevelSecurity.IdsAllowedToRead;

        return rowLevelSecurity;
    }

    private async Task UpdateClientInReport(string reportFileId, string orgId)
    {
        var clientIds = GetClientIds(orgId);
        var report = await _repository.GetItemAsync<PraxisReport>(p => p.ReportFileId.Equals(reportFileId));
        report.ClientIds = clientIds;
        await _repository.UpdateAsync(p => p.ReportFileId.Equals(reportFileId), report);
    }
}