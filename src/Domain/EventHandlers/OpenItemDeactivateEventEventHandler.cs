using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using MongoDB.Driver;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class OpenItemDeactivateEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<OpenItemDeactivateEventHandler> _logger;
        private readonly IPraxisOpenItemService _praxisOpenItemService;
        private readonly IRepository _repository;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

        public OpenItemDeactivateEventHandler(
            ILogger<OpenItemDeactivateEventHandler> logger,
            IPraxisOpenItemService praxisOpenItemService,
            IRepository repository,
            ICockpitSummaryCommandService cockpitSummaryCommandService
        )
        {
            _logger = logger;
            _praxisOpenItemService = praxisOpenItemService;
            _repository = repository;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {HandlerName} -> with payload {Payload}.",
                nameof(OpenItemDeactivateEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            if (@event != null && @event.EventType.Equals(PraxisEventType.OpenItemDeactivateEvent))
            {
                try
                {
                    var openItemIds = JsonConvert.DeserializeObject<List<string>>(@event.JsonPayload);
                    if (openItemIds?.Count > 0)
                    {
                        var openItems = _repository.GetItems<PraxisOpenItem>(m => openItemIds.Contains(m.ItemId) && !m.IsMarkedToDelete).ToList();
                        foreach (var openItem in openItems)
                        {
                            if (!string.IsNullOrEmpty(openItem.TaskSchedule?.ItemId))
                            {
                                await _praxisOpenItemService.UpdateTaskForToDo(
                                    new
                                    {
                                        TaskScheduleId = openItem?.TaskSchedule?.ItemId,
                                        RelatedEntityObject = new
                                        {
                                            IsActive = false
                                        },
                                        HasTaskScheduleIntoRelatedEntity = true
                                    }
                                );

                                await _cockpitSummaryCommandService.UpdateCockpitSummary(new string[] { openItem?.TaskSchedule?.ItemId }, EntityName.PraxisOpenItem);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Operation aborted as payload is empty.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception occured during {nameof(@event.EventType)} event handle.");
                    _logger.LogError("Exception Message: {Message}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
                }

                _logger.LogInformation("Handled by: {HandlerName}.", nameof(OpenItemDeactivateEventHandler));
            }

            return response;
        }
    }
}
