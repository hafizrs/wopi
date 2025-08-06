using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemConfigEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemEvents
{
    public class PraxisOpenItemUpdatedEventHandler : IBaseEventHandlerAsync<GqlEvent<PraxisOpenItem>>
    {
        private readonly ILogger<PraxisOpenItemConfigCreatedEventHandler> _logger;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly IRepository _repository;
        private readonly IGenericEventPublishService _genericEventPublishService;
        private readonly IDependencyManagementService _dependencyManagementService;
        public PraxisOpenItemUpdatedEventHandler(
            ILogger<PraxisOpenItemConfigCreatedEventHandler> logger, 
            ICockpitSummaryCommandService cockpitSummaryCommandService, 
            IRepository repository, 
            IGenericEventPublishService genericEventPublishService, 
            IDependencyManagementService dependencyManagementService
            )
        {
            _logger = logger;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _repository = repository;
            _genericEventPublishService = genericEventPublishService;
            _dependencyManagementService = dependencyManagementService;
        }
        public async Task<bool> HandleAsync(GqlEvent<PraxisOpenItem> eventPayload)
        {
            _logger.LogInformation("Entered into the {HandlerName} with Payload: {Payload} ItemId: {ItemId}", nameof(PraxisOpenItemUpdatedEventHandler), JsonConvert.SerializeObject(eventPayload), eventPayload.Filter);
            try
            {
                var openItem = _repository.GetItem<PraxisOpenItem>(oi => oi.ItemId.Equals(eventPayload.Filter) && !oi.IsMarkedToDelete);
                _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(openItem);
                if (openItem == null)
                {
                    _logger.LogError("OpenItem with id {OpenItemId} not found", eventPayload.Filter);
                    return false;
                }
                if (openItem.IsMarkedToDelete)
                {
                    await _cockpitSummaryCommandService.DeleteSummaryAsync(new List<string> { eventPayload.Filter }, CockpitTypeNameEnum.PraxisOpenItem);
                    await _dependencyManagementService.HandleTodoDeletionAsync(new List<string> { openItem.ItemId });
                    _genericEventPublishService.PublishDmsArtifactUsageReferenceDeleteEvent(openItem);
                    return true;
                }
                if (!string.IsNullOrEmpty(openItem.TaskReference?.Value))
                {
                    var isSummaryExist = await _repository.ExistsAsync<RiqsTaskCockpitSummary>(s =>
                        s.RelatedEntityId == openItem.ItemId &&
                        s.RelatedEntityName == CockpitTypeNameEnum.PraxisOpenItem);
                    await _cockpitSummaryCommandService.CreateSummary(eventPayload.Filter, nameof(PraxisOpenItem), isSummaryExist);
                }
                if (!string.IsNullOrEmpty(openItem?.TaskSchedule?.ItemId)) await _cockpitSummaryCommandService.UpdateCockpitSummary(new string[] { openItem?.TaskSchedule?.ItemId }, EntityName.PraxisOpenItem);
                var isInactivated = openItem.IsActive is false;
                if (isInactivated)
                {
                    await _dependencyManagementService.HandleTodoInactivationAsync(new List<string> { openItem.ItemId });
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisOpenItemUpdatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }
    }
}
