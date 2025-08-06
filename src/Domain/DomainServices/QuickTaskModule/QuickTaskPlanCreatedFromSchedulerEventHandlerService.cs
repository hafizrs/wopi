using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.QuickTaskModule
{
    public class QuickTaskPlanCreatedFromSchedulerEventHandlerService : IQuickTaskPlanCreatedFromSchedulerEventHandlerService
    {
        private readonly ILogger<QuickTaskPlanCreatedFromSchedulerEventHandlerService> _logger;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        private readonly IRepository _repository;

        public QuickTaskPlanCreatedFromSchedulerEventHandlerService(
            ILogger<QuickTaskPlanCreatedFromSchedulerEventHandlerService> logger,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
            IRepository repository)
        {
            _logger = logger;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
            _repository = repository;
        }

        public async Task InitiateCockpitGenerationService(List<string> quickTaskPlanIds)
        {
            _logger.LogInformation("Initiating cockpit generation service for quick task plan ids: {QuickTaskPlanIds}", quickTaskPlanIds);
            try
            {
                var taskList = quickTaskPlanIds.Select(async quickTaskPlanId =>
                {
                    var quickTaskPlan = await _repository.GetItemAsync<RiqsQuickTaskPlan>(s =>
                        s.ItemId == quickTaskPlanId);
                    var files = quickTaskPlan?.QuickTaskShift?.TaskList?
                        .SelectMany(t => t.LibraryForms?.Select(f => f?.LibraryFormId?.ToString()) ?? new List<string>())
                        .Where(f => !string.IsNullOrEmpty(f)).ToList() ?? new List<string>();
                    var activityName = $"{CockpitDocumentActivityEnum.PENDING_FORMS_TO_SIGN}";
                    if (files.Any())
                    {
                        await _cockpitDocumentActivityMetricsGenerationService
                            .OnDocumentUsedInShiftPlanGenerateActivityMetrics(files.ToArray(), activityName, quickTaskPlanId);
                    }
                }).ToList();
                await Task.WhenAll(taskList);
                await _cockpitSummaryCommandService.DeleteSummaryAsync(quickTaskPlanIds, CockpitTypeNameEnum.RiqsShiftPlan);
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while initiating cockpit generation service for quick task plan ids: {QuickTaskPlanIds}. Error Message: {Message}. Error Details: {StackTrace}", quickTaskPlanIds, e.Message, e.StackTrace);
            }
        }
    }
} 