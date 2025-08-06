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

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class MaintenanceMailSendEventEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<MaintenanceMailSendEventEventHandler> _logger;
        private readonly IPraxisEquipmentMaintenanceService _praxisEquipmentMaintenanceService;
        private readonly IRepository _repository;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

        public MaintenanceMailSendEventEventHandler(
            ILogger<MaintenanceMailSendEventEventHandler> logger,
            IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService,
            IRepository repository,
            ICockpitSummaryCommandService cockpitSummaryCommandService
        )
        {
            _logger = logger;
            _praxisEquipmentMaintenanceService = praxisEquipmentMaintenanceService;
            _repository = repository;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {HandlerName} -> with payload {Payload}.",
                nameof(MaintenanceMailSendEventEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            if (@event != null && @event.EventType.Equals(PraxisEventType.MaintenanceMailSendEvent))
            {
                try
                {
                    var maintenanceId = JsonConvert.DeserializeObject<string>(@event.JsonPayload);

                    if (!string.IsNullOrWhiteSpace(maintenanceId))
                    {
                        var maintenance = await _repository.GetItemAsync<PraxisEquipmentMaintenance>(m => m.ItemId == maintenanceId && !m.IsMarkedToDelete);
                        if (maintenance != null)
                        {
                            response = await _praxisEquipmentMaintenanceService.ProcessEmailForResponsibleUsers(maintenance);
                            await _cockpitSummaryCommandService.CreateSummary(maintenance?.ItemId, nameof(CockpitTypeNameEnum.PraxisEquipmentMaintenance));
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

                _logger.LogInformation("Handled by: {HandlerName}.", nameof(MaintenanceMailSendEventEventHandler));
            }

            return response;
        }
    }
}
