using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SKO;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.EquipmentEvents
{
    public class PraxisEquipmentUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisEquipment>>
    {
        private readonly ILogger<PraxisEquipmentUpdatedEventHandler> _logger;
        private readonly IPraxisEquipmentMaintenanceService _praxisEquipmentMaintenanceService;
        private readonly IRepository _repository;
        private readonly IGenericEventPublishService _genericEventPublishService;
        private readonly IServiceClient _serviceClient;
        private readonly IPraxisEquipmentService _praxisEquipmentService;
        public PraxisEquipmentUpdatedEventHandler(
            ILogger<PraxisEquipmentUpdatedEventHandler> logger,
            IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService,
            IRepository repository,
            IServiceClient serviceClient,
            IGenericEventPublishService genericEventPublishService,
            IPraxisEquipmentService praxisEquipmentService
        )
        {
            _logger = logger;
            _praxisEquipmentMaintenanceService = praxisEquipmentMaintenanceService;
            _repository = repository;
            _serviceClient = serviceClient;
            _genericEventPublishService = genericEventPublishService;
            _praxisEquipmentService = praxisEquipmentService;
        }
        public bool Handle(GqlEvent<PraxisEquipment> data)
        {
            try
            {
                if (!string.IsNullOrEmpty(data?.Filter) && data.EventData != null)
                {
                    var equipment = _repository.GetItem<PraxisEquipment>(e => e.ItemId.Equals(data.Filter));
                    _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(equipment);
                    
                    _praxisEquipmentService.GenerateQrFileForEquipment(equipment).GetAwaiter().GetResult();
                    
                    var maintenanceIds = _repository.GetItems<PraxisEquipmentMaintenance>
                        (m => !m.IsMarkedToDelete && m.PraxisEquipmentId == data.Filter)
                        .Select(m => m.ItemId)?.ToList() ?? new List<string>();

                    foreach (var maintenanceId in maintenanceIds)
                    {
                        _praxisEquipmentMaintenanceService.AssignTasks(maintenanceId, false);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisEquipmentUpdatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }
    }
}
