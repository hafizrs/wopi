using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Newtonsoft.Json;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.EquipmentEvents
{
    public class PraxisEquipmentCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisEquipment>>
    {
        private readonly IPraxisEquipmentService praxisEquipmentService;
        private readonly ILogger<PraxisEquipmentCreatedEventHandler> _logger;
        private readonly IRepository repository;
        public PraxisEquipmentCreatedEventHandler(
            IPraxisEquipmentService praxisEquipmentService,
            ILogger<PraxisEquipmentCreatedEventHandler> log,
            IRepository repo)
        {
            this.praxisEquipmentService = praxisEquipmentService;
            this._logger = log;
            this.repository = repo;
        }
        public bool Handle(GqlEvent<PraxisEquipment> eventPayload)
        {
            try
            {
                _logger.LogInformation("Entered PraxisEquipmentCreatedEventHandler");
                var equipmentForQrCode = repository.GetItem<PraxisEquipment>(e => e.ItemId == eventPayload.EntityData.ItemId);
                
                if(string.IsNullOrEmpty(equipmentForQrCode.EquipmentQrFileId))
                {
                    _logger.LogInformation("Going to GenerateQrFileForEquipment");
                    praxisEquipmentService.GenerateQrFileForEquipment(equipmentForQrCode).GetAwaiter().GetResult();
                }
                _logger.LogInformation("Going to AddRowLevelSecurity with eventpayload: {EventPayload}", JsonConvert.SerializeObject(eventPayload));
                praxisEquipmentService.AddRowLevelSecurity(eventPayload.EntityData.ItemId, eventPayload.EntityData.ClientId);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisEquipmentCreatedEventHandler -> {Message}", e.Message);
            }

            return false;
        }
    }
}
