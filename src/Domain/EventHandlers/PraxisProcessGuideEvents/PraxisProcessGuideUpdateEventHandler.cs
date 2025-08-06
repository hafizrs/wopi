using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisProcessGuideEvents
{
    public class PraxisProcessGuideUpdateEventHandler : IBaseEventHandlerAsync<GqlEvent<PraxisProcessGuide>>
    {
        private readonly ILogger<PraxisProcessGuideUpdateEventHandler> _logger;
        private readonly IPraxisProcessGuideService _processGuideService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly IRepository _repository;
        private readonly IDependencyManagementService _dependencyManagementService;
        public PraxisProcessGuideUpdateEventHandler(ILogger<PraxisProcessGuideUpdateEventHandler> logger,
            IPraxisProcessGuideService processGuideService,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            IRepository repository,
            IDependencyManagementService dependencyManagementService
            )
        {
            _logger = logger;
            _processGuideService = processGuideService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _repository = repository;
            _dependencyManagementService = dependencyManagementService;
        }

        public async Task<bool> HandleAsync(GqlEvent<PraxisProcessGuide> payload)
        {
            _logger.LogInformation("Enter into the {HandlerName} with Payload: {Payload} ItemId: {ItemId}", nameof(PraxisProcessGuideUpdateEventHandler), JsonConvert.SerializeObject(payload), payload.Filter);
            try
            {
                await HandleCockpitSummary(payload.Filter);
                await _processGuideService.UpdateProcessGuideCompletionStatus(new List<string> {payload.EntityData.ItemId});

                return true;
            }
            catch(Exception ex)
            {
                _logger.LogError("Exception occurred in event handler {EventHandlerName} with exception -> {ExceptionMessage}",
                    nameof(PraxisProcessGuideUpdateEventHandler), ex.Message);
                return false;
            }
        }

        private async Task HandleCockpitSummary(string processGuideId)
        {
            var processGuide = _repository.GetItem<PraxisProcessGuide>(pg => pg.ItemId.Equals(processGuideId) && !pg.IsMarkedToDelete);
            if (processGuide == null)
            {
                _logger.LogWarning("ProcessGuide with id {ProcessGuideId} not found", processGuideId);
                return;
            }

            if (processGuide.IsActive is false)
            {
                await _dependencyManagementService.HandleGuideInactivationAsync(new List<string> { processGuideId });
            }

            if (processGuide.IsMarkedToDelete)
            {
                await _cockpitSummaryCommandService.DeleteSummaryAsync(new List<string> { processGuideId }, CockpitTypeNameEnum.PraxisProcessGuide);
                await _dependencyManagementService.HandleGuideDeletionAsync(new List<string> { processGuideId });
            }

            if (!processGuide.IsATemplate)
            {
                var isSummaryExists = await _repository.ExistsAsync<RiqsTaskCockpitSummary>(s =>
                    s.RelatedEntityId == processGuideId &&
                    s.RelatedEntityName == CockpitTypeNameEnum.PraxisProcessGuide);
                await _cockpitSummaryCommandService.CreateSummary(processGuideId, nameof(PraxisProcessGuide), isSummaryExists);
            }
        }
    }
}
