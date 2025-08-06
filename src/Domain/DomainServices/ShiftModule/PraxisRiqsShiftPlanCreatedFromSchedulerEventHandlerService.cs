using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;

public class PraxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService : IPraxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService
{
    private readonly ILogger<PraxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService> _logger;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
    private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
    private readonly IRepository _repository;

    public PraxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService(
        ILogger<PraxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService> logger,
        ICockpitSummaryCommandService cockpitSummaryCommandService,
        ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
        IRepository repository)
    {
        _logger = logger;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
        _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
        _repository = repository;
    }

    public async Task InitiateCockpitGenerationService(List<string> shiftPlanIds)
    {
        _logger.LogInformation("Initiating cockpit generation service for shift plan ids: {ShiftPlanIds}", shiftPlanIds);
        try
        {
            var taskList = shiftPlanIds.Select(async shiftPlanId =>
            {
                var shift = await _repository.GetItemAsync<RiqsShiftPlan>(s =>
                    s.ItemId == shiftPlanId);
                var files = shift?.Shift?.LibraryForms?
                    .Select(f => f.LibraryFormId).ToList() ?? new List<string>();
                var activityName = $"{CockpitDocumentActivityEnum.PENDING_FORMS_TO_SIGN}";
                if (files.Any())
                {
                    await _cockpitDocumentActivityMetricsGenerationService
                        .OnDocumentUsedInShiftPlanGenerateActivityMetrics(files.ToArray(), activityName, shiftPlanId);
                }
            }).ToList();
            await Task.WhenAll(taskList);
            await _cockpitSummaryCommandService.DeleteSummaryAsync(shiftPlanIds, CockpitTypeNameEnum.RiqsShiftPlan);
        }
        catch (Exception e)
        {
            _logger.LogError("Error occured while initiating cockpit generation service for shift plan ids: {ShiftPlanIds}. Error Message: {Message}. Error Details: {StackTrace}", shiftPlanIds, e.Message, e.StackTrace);
        }
    }
}