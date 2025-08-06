using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemEvents
{
    public class PraxisOpenItemCreatedEventHandler : IBaseEventHandlerAsync<GqlEvent<PraxisOpenItem>>
    {
        private readonly ILogger<PraxisOpenItemCreatedEventHandler> _logger;
        private readonly IRepository _repository;
        private readonly IPraxisOpenItemService _praxisOpenItemService;
        private readonly ICirsOpenItemAttachmentService _cirsOpenItemAttachmentService;
        private readonly ICockpitFormDocumentActivityMetricsGenerationService _cockpitFormDocumentActivityMetricsGenerationService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly INotificationService _notificationService;

        public PraxisOpenItemCreatedEventHandler(
            IRepository repository,
            ILogger<PraxisOpenItemCreatedEventHandler> logger,
            IPraxisOpenItemService praxisOpenItemService,
            ICirsOpenItemAttachmentService cirsOpenItemAttachmentService,
            ICockpitFormDocumentActivityMetricsGenerationService cockpitFormDocumentActivityMetricsGenerationService,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            INotificationService notificationService)
        {
            _logger = logger;
            _praxisOpenItemService = praxisOpenItemService;
            _repository = repository;
            _cirsOpenItemAttachmentService = cirsOpenItemAttachmentService;
            _cockpitFormDocumentActivityMetricsGenerationService = cockpitFormDocumentActivityMetricsGenerationService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _notificationService = notificationService;
        }
        public async Task<bool> HandleAsync(GqlEvent<PraxisOpenItem> eventPayload)
        {
            try
            {
                var existingOpenItem = _repository.GetItem<PraxisOpenItem>(o => o.ItemId == eventPayload.EntityData.ItemId);
                _praxisOpenItemService.AddPraxisOpenItemRowLevelSecurity(
                    eventPayload.EntityData.ItemId, eventPayload.EntityData.ClientId
                );
                if (existingOpenItem?.TaskReferenceId != null)
                {
                    await _praxisOpenItemService.GetOpenItemCompletionDetails(new GetCompletionListQuery { TaskReferenceId = existingOpenItem.TaskReferenceId });
                    
                    if (existingOpenItem.TaskReference?.Value?.Equals("Reporting", StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        await _cirsOpenItemAttachmentService.UpdateCirsOnOpenItemCreate(existingOpenItem.TaskReferenceId, existingOpenItem.ItemId);
                    }

                    if (existingOpenItem.TaskReference?.Value?.Equals("Form", StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        await _cockpitFormDocumentActivityMetricsGenerationService.OnCreateFormTodoGenerateActivityMetrics(existingOpenItem);
                    }
                    if (!string.IsNullOrEmpty(existingOpenItem.TaskReference?.Value))
                    {
                        var isSummaryExist = await _repository.ExistsAsync<RiqsTaskCockpitSummary>(s =>
                            s.RelatedEntityId == existingOpenItem.ItemId &&
                            s.RelatedEntityName == CockpitTypeNameEnum.PraxisOpenItem);
                        await _cockpitSummaryCommandService.CreateSummary(existingOpenItem.ItemId, nameof(PraxisOpenItem), isSummaryExist);
                    }

                    if (!string.IsNullOrEmpty(eventPayload?.EntityData?.ItemId))
                    {
                        var denormalizePayload = JsonConvert.SerializeObject(new
                        {
                            OpenItemId = eventPayload?.EntityData?.ItemId
                        });
                        await _notificationService.GetCommonSubscriptionNotification(
                            true,
                            eventPayload.EntityData.OpenItemConfigId,
                            "OpenItemCreated",
                            "OpenItemCreated",
                            denormalizePayload
                        );
                    }
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
