using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ChangeEvents;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.RiskManagementEvents
{
    public class PraxisRiskUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisRisk>>
    {
        private readonly ILogger<PraxisRiskUpdatedEventHandler> _logger;
        private readonly IPraxisRiskService praxisRiskService;
        private readonly IChangeLogService changeLogService;

        public PraxisRiskUpdatedEventHandler(
            ILogger<PraxisRiskUpdatedEventHandler> logger,
            IPraxisRiskService praxisRiskService,
            IChangeLogService changeLogService
        )
        {
            _logger = logger;
            this.praxisRiskService = praxisRiskService;
            this.changeLogService = changeLogService;
        }
        public bool Handle(GqlEvent<PraxisRisk> eventPayload)
        {
            try
            {
                praxisRiskService.UpdateRecentAssessment(eventPayload.Filter);
                praxisRiskService.UpdateAttachmentInReporting(eventPayload.EntityData.ItemId).GetAwaiter().GetResult();

                if (eventPayload.EventData != null)
                {
                    PraxisRiskChangeEvent clientChanges =
                        JsonConvert.DeserializeObject<PraxisRiskChangeEvent>(eventPayload.EventData);

                    if (clientChanges != null)
                    {
                        return ProcessRiskChanges(clientChanges).GetAwaiter().GetResult();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisRiskUpdatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }

        private async Task<bool> ProcessRiskChanges(PraxisRiskChangeEvent changes)
        {
            var riskNameUpdateStatuses = new List<Task<bool>>();

            if (!string.IsNullOrEmpty(changes.Reference) && !string.IsNullOrEmpty(changes.ItemId))
            {
                var updates = new Dictionary<string, object>
                {
                    {"TaskReferenceTitle", changes.Reference }
                };

                var builders = Builders<BsonDocument>.Filter;
                var dataFilters = builders.Eq("TaskReferenceId", changes.ItemId);

                var praxisOpenItemUpdateStatus = changeLogService.UpdateChange(EntityName.PraxisOpenItem, dataFilters, updates);
                riskNameUpdateStatuses.Add(praxisOpenItemUpdateStatus);

                var openItemConfigupdates = new Dictionary<string, object>
                {
                    {"TaskReferenceTitle", changes.Reference }
                };

                var praxisOpenItemConfigUpdateStatus = changeLogService.UpdateChange(EntityName.PraxisOpenItemConfig, dataFilters, openItemConfigupdates);
                riskNameUpdateStatuses.Add(praxisOpenItemConfigUpdateStatus);
            }

            await Task.WhenAll(riskNameUpdateStatuses);

            return true;
        }
    }
}
