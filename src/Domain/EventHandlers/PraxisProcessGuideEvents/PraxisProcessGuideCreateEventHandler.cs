using MassTransit.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisProcessGuideEvents
{
    public class PraxisProcessGuideCreateEventHandler : IBaseEventHandlerAsync<GqlEvent<PraxisProcessGuide>>
    {
        private readonly ILogger<PraxisProcessGuideCreateEventHandler> _logger;
        private readonly IPraxisProcessGuideService _processGuideService;
        private readonly IPraxisEquipmentMaintenanceService _praxisEquipmentMaintenanceService;
        private readonly IShiftTaskAssignService _shiftTaskAssignService;
        private readonly IRepository _repository;
        private readonly IPraxisAssignedTaskFormService _praxisAssignedTaskFormService;
        private readonly ICirsProcessGuideAttachmentService _cirsProcessGuideAttachmentService;
        private readonly INotificationService _notificationService;
        private readonly ICockpitFormDocumentActivityMetricsGenerationService _cockpitFormDocumentActivityMetricsGenerationService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly IDependencyManagementService _dependencyManagementService;
        private readonly IQuickTaskAssignService _quickTaskAssignService;

        public PraxisProcessGuideCreateEventHandler(
            ILogger<PraxisProcessGuideCreateEventHandler> logger,
            IPraxisProcessGuideService processGuideService,
            IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService,
            IRepository repository,
            IShiftTaskAssignService shiftTaskAssignService,
            IPraxisAssignedTaskFormService praxisAssignedTaskFormService,
            ICirsProcessGuideAttachmentService cirsProcessGuideAttachmentService,
            INotificationService notificationService,
            ICockpitFormDocumentActivityMetricsGenerationService cockpitFormDocumentActivityMetricsGenerationService,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            IDependencyManagementService dependencyManagementService,
            IQuickTaskAssignService quickTaskAssignService
        )
        {
            _logger = logger;
            _processGuideService = processGuideService;
            _praxisEquipmentMaintenanceService = praxisEquipmentMaintenanceService;
            _shiftTaskAssignService = shiftTaskAssignService;
            _praxisAssignedTaskFormService = praxisAssignedTaskFormService;
            _repository = repository;
            _cirsProcessGuideAttachmentService = cirsProcessGuideAttachmentService;
            _notificationService = notificationService;
            _cockpitFormDocumentActivityMetricsGenerationService = cockpitFormDocumentActivityMetricsGenerationService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _dependencyManagementService = dependencyManagementService;
            _quickTaskAssignService = quickTaskAssignService;
        }

        public async Task<bool> HandleAsync(GqlEvent<PraxisProcessGuide> payload)
        {
            _logger.LogInformation("Enter into the {EventHandlerName} with clientId -> {ClientId}",
                nameof(PraxisProcessGuideCreateEventHandler), payload.EntityData.ClientId);
            try
            {
                var entityItemId = payload.EntityData.ItemId;
                var formId = payload.EntityData.FormId;

                if (!string.IsNullOrEmpty(payload?.EntityData?.RelatedEntityId))
                {
                    await UpdateRelatedEntityAsync(payload.EntityData);
                }
                await _processGuideService.UpdateRowLevelSecurity(payload.EntityData.ItemId);
                await _processGuideService.UpdateProcessGuideCompletionStatus(new List<string> { entityItemId });


                _praxisAssignedTaskFormService.CreateAssignedForm(
                    formId,
                    nameof(PraxisProcessGuide),
                    entityItemId);
                
                var processGuide = await _repository.GetItemAsync<PraxisProcessGuide>(pg => pg.ItemId == entityItemId);
                await _cockpitFormDocumentActivityMetricsGenerationService.OnCreatePraxisProcessGuideFormGenerateActivityMetrics(processGuide);

                var denormalizePayload = JsonConvert.SerializeObject(new
                {
                    ProcessGuideId = entityItemId
                });
                await _notificationService.GetCommonSubscriptionNotification(
                    true, 
                    payload.EntityData.PraxisProcessGuideConfigId,
                    "ProcessGuideCreated",
                    "ProcessGuideCreated",
                    denormalizePayload
                );

                await HandleCockpitSummary(entityItemId);

                return true;
            }
            catch(Exception ex)
            {
                _logger.LogError("Exception occurred in event handler {EventHandlerName} with exception -> {ExceptionMessage}",
                    nameof(PraxisProcessGuideCreateEventHandler), ex.Message);
                return false;
            }
        }

        private async Task UpdateRelatedEntityAsync(PraxisProcessGuide processGuide)
        {
            var relatedEntityName = processGuide.RelatedEntityName;
            var relatedEntityId = processGuide.RelatedEntityId;
            if (string.IsNullOrEmpty(relatedEntityId)) return;

            var formId = processGuide.FormId;
            var entityItemId = processGuide.ItemId;
            var relatedEntityIds = new List<string>() { relatedEntityId };
            var relatedEntityIdsAsString = processGuide.MetaDataList?.Find(m => m.Key == "RelatedEntityIds")?.MetaData?.Value ?? JsonConvert.SerializeObject(new List<string>());
            if (!string.IsNullOrEmpty(relatedEntityIdsAsString))
            {
                relatedEntityIds.AddRange(JsonConvert.DeserializeObject<List<string>>(relatedEntityIdsAsString));
                relatedEntityIds = relatedEntityIds.Distinct().ToList();
            }

            var task = relatedEntityName switch
            {
                nameof(PraxisEquipmentMaintenance) => _praxisEquipmentMaintenanceService
                    .UpdateMaintenanceForProcessGuideCreated(relatedEntityId, entityItemId),
                nameof(RiqsShiftPlan) => _shiftTaskAssignService
                    .UpdateShiftPlanForProcessGuideCreated(relatedEntityIds, entityItemId),
                nameof(RiqsQuickTaskPlan) => _quickTaskAssignService
                    .UpdateQuickTaskPlanForProcessGuideCreated(relatedEntityIds, entityItemId, formId),
                nameof(CirsGenericReport) => _cirsProcessGuideAttachmentService
                    .UpdateOnProcessGuideCreatedAsync(relatedEntityId, entityItemId),
                _ => null
            };

            if (task != null) await task;
        }

        private async Task HandleCockpitSummary(string processGuideId)
        {
            var processGuide = _repository.GetItem<PraxisProcessGuide>(pg => pg.ItemId.Equals(processGuideId) && !pg.IsMarkedToDelete);
            if (processGuide == null)
            {
                _logger.LogError("ProcessGuide with id {ProcessGuideId} not found", processGuideId);
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
