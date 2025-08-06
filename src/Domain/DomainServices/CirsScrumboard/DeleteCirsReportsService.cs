using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

public class DeleteCirsReportsService : IDeleteCirsReportsService
{
    private readonly IRepository _repository;
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

    public DeleteCirsReportsService(
        IRepository repository,
        ISecurityContextProvider securityContextProvider,
        ICockpitSummaryCommandService cockpitSummaryCommandService)
    {
        _repository = repository;
        _securityContextProvider = securityContextProvider;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
    }

    public async Task<bool> InitiateDeletionAsync(DeleteCirsReportsCommand command)
    {
        var cirsReports = GetCirsReportsByIds(command.CirsReportIds);

        if (command.CirsReportIds.Count == cirsReports.Count)
        {
            await DeleteCirsReports(command.CirsReportIds);
            await _cockpitSummaryCommandService.DeleteSummaryAsync(command.CirsReportIds,
                CockpitTypeNameEnum.CirsGenericReport);
        }

        return true;
    }

    private List<string> GetCirsReportsByIds(List<string> cirsReportIds)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();

        return
            _repository
            .GetItems<CirsGenericReport>(i =>
                cirsReportIds.Contains(i.ItemId) &&
                !i.IsMarkedToDelete)
            .Select(i => i.ItemId)
            .ToList();
    }

    private async Task<bool> DeleteCirsReports(List<string> cirsReportIds)
    {
        var deletionTasks = new List<Task>();

        cirsReportIds.ForEach(cirsReportId =>
        {
            deletionTasks.Add(
                _repository.DeleteAsync<CirsGenericReport>(
                    report => report.ItemId == cirsReportId)
            );
        });

        await Task.WhenAll(deletionTasks);

        return true;
    }

    public async Task DeleteDataForClient(string clientId, string orgId = null)
    {
        var deleteTasks = new List<Task>
        {
            _repository.DeleteAsync<CirsDashboardPermission>(cirs => cirs.PraxisClientId.Equals(clientId)),
            _repository.DeleteAsync<CirsGenericReport>(cirs => cirs.AffectedInvolvedParties != null && cirs.AffectedInvolvedParties.Any(c => c.PraxisClientId.Equals(clientId)))
        };
        await Task.WhenAll(deleteTasks);
    }
}