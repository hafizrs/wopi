using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.EquipmentMaintenanceEvents
{
    public class EquipmentMaintenanceUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisEquipmentMaintenance>>
    {
        private readonly IRepository repository;
        private readonly ILogger<EquipmentMaintenanceUpdatedEventHandler> _logger;
        private readonly IEmailNotifierService emailNotifierService;
        private readonly IEmailDataBuilder emailDataBuilder;
        private readonly IPraxisEquipmentMaintenanceService _praxisEquipmentMaintenanceService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly IGenericEventPublishService _genericEventPublishService;
        private readonly IPraxisReportTemplateService _praxisReportTemplateService;

        public EquipmentMaintenanceUpdatedEventHandler(
            IRepository repository,
            ILogger<EquipmentMaintenanceUpdatedEventHandler> logger,
            IEmailNotifierService emailNotifierService,
            IEmailDataBuilder emailDataBuilder,
            IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            IGenericEventPublishService genericEventPublishService,
            IPraxisReportTemplateService praxisReportTemplateService
        )
        {
            this.repository = repository;
            _logger = logger;
            this.emailNotifierService = emailNotifierService;
            this.emailDataBuilder = emailDataBuilder;
            _praxisEquipmentMaintenanceService = praxisEquipmentMaintenanceService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _genericEventPublishService = genericEventPublishService;
            _praxisReportTemplateService = praxisReportTemplateService;
        }

        public bool Handle(GqlEvent<PraxisEquipmentMaintenance> eventPayload)
        {
            try
            {
                if (eventPayload?.EntityData != null)
                {
                    if (string.IsNullOrEmpty(eventPayload.EntityData.ItemId)) eventPayload.EntityData.ItemId = eventPayload.Filter;

                    var maintenance = repository.GetItem<PraxisEquipmentMaintenance>(pem =>
                        pem.ItemId.Equals(eventPayload.EntityData.ItemId));

                    _praxisEquipmentMaintenanceService.UpdatePraxisEquipmentMaintenanceDates(eventPayload.Filter, eventPayload.EntityData).GetAwaiter().GetResult();

                    var mailSendDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
                    if (mailSendDate >= maintenance.MaintenanceDate)
                    {
                        _ = _praxisEquipmentMaintenanceService.ProcessEmailForResponsibleUsers(eventPayload.EntityData).GetAwaiter().GetResult();
                        _cockpitSummaryCommandService.CreateSummary(maintenance.ItemId,
                        nameof(CockpitTypeNameEnum.PraxisProcessGuide), true).GetAwaiter();
                    }
                    
                    maintenance = repository.GetItem<PraxisEquipmentMaintenance>(pem =>
                        pem.ItemId.Equals(eventPayload.EntityData.ItemId));
                    _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(maintenance);

                    _praxisReportTemplateService.OnMaintenanceUpdateAdjustReportStatus(maintenance.ItemId).GetAwaiter();
                }

                if (
                    !string.IsNullOrEmpty(eventPayload?.EntityData?.PraxisFormInfo?.FormId) && eventPayload?.EntityData?.MaintenanceDate != null
                    && DateTime.UtcNow.Date >= eventPayload.EntityData.MaintenanceDate.Date
                )
                {
                    _praxisEquipmentMaintenanceService.AssignTasks(eventPayload.Filter, true);
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("EquipmentMaintenanceUpdatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }
    }
}
