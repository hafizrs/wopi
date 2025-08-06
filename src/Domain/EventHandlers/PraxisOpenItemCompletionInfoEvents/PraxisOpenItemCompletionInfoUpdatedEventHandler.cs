﻿using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;

using System;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemCompletionInfoEvents
{
    public class
        PraxisOpenItemCompletionInfoUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisOpenItemCompletionInfo>>
    {
        private readonly ILogger<PraxisOpenItemCompletionInfoUpdatedEventHandler> _logger;
        private readonly IRepository _repository;
        private readonly IPraxisOpenItemService _praxisOpenItemService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

        public PraxisOpenItemCompletionInfoUpdatedEventHandler(
            ILogger<PraxisOpenItemCompletionInfoUpdatedEventHandler> logger,
            IRepository repository,
            IPraxisOpenItemService praxisOpenItemService,
            ICockpitSummaryCommandService cockpitSummaryCommandService)
        {
            _logger = logger;
            _repository = repository;
            _praxisOpenItemService = praxisOpenItemService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
        }

        public bool Handle(GqlEvent<PraxisOpenItemCompletionInfo> eventPayload)
        {
            _logger.LogInformation(
                $"Enter {nameof(PraxisOpenItemCompletionInfoUpdatedEventHandler)} with event payload: {JsonConvert.SerializeObject(eventPayload)}.");
            try
            {
                var itemId = eventPayload.Filter;
                var praxisOpenItem = _repository.GetItem<PraxisOpenItem>(openItem =>
                    openItem.ItemId.Equals(eventPayload.EntityData.PraxisOpenItemId) && !openItem.IsMarkedToDelete
                );
                if (praxisOpenItem != null && itemId != null)
                {
                    var praxisOpenItemCompletionInfo = _repository.GetItem<PraxisOpenItemCompletionInfo>(
                        completionInfo => completionInfo.ItemId == itemId && !completionInfo.IsMarkedToDelete
                    );

                    if (praxisOpenItemCompletionInfo != null && praxisOpenItemCompletionInfo.Completion != null)
                    {
                        _praxisOpenItemService.UpdatePraxisOpenItemCompletionStatus( 
                            praxisOpenItem, praxisOpenItemCompletionInfo, false
                        );
                        _cockpitSummaryCommandService.SyncSubmittedAnswer(itemId, nameof(PraxisOpenItemCompletionInfo))
                            .GetAwaiter()
                            .GetResult();
                    }
                }
                else
                {
                    _logger.LogInformation(
                        $"No {nameof(PraxisOpenItem)} data found " +
                        $"by ItemId: {eventPayload.EntityData.PraxisOpenItemId}."
                    );
                }

                _logger.LogInformation(
                    $"Handled by {nameof(PraxisOpenItemCompletionInfoUpdatedEventHandler)} " +
                    $"with event payload: {JsonConvert.SerializeObject(eventPayload)}."
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured during updating completion status in: {nameof(PraxisOpenItem)} " +
                    $"with ItemId: {eventPayload.EntityData.PraxisOpenItemId}  " +
                    $"Exception Message: {ex.Message}. Exception Details: {ex.StackTrace}."
                );
                return false;
            }
        }
    }
}