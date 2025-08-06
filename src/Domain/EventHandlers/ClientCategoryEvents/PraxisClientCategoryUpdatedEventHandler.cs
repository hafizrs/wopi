using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ChangeEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.ClientCategoryEvents
{
    public class PraxisClientCategoryUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisClientCategory>>
    {
        private readonly IChangeLogService changeLogService;
        private readonly ILogger<PraxisClientCategoryUpdatedEventHandler> log;
        public PraxisClientCategoryUpdatedEventHandler(ILogger<PraxisClientCategoryUpdatedEventHandler> log, 
            IChangeLogService changeLogService
        )
        {
            this.changeLogService = changeLogService;
            this.log = log;
        }
        public bool Handle(GqlEvent<PraxisClientCategory> eventPayload)
        {
            try
            {
                if (eventPayload.EventData != null)
                {
                    PraxisClientCategoryChangeEvent categoryChanges =
                        JsonConvert.DeserializeObject<PraxisClientCategoryChangeEvent>(eventPayload.EventData);

                    if (categoryChanges != null)
                    {
                        return ProcessCategoryChanges(categoryChanges).GetAwaiter().GetResult();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                log.LogError("PraxisClientCategoryUpdatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }

        private async Task<bool> ProcessCategoryChanges(PraxisClientCategoryChangeEvent changes)
        {
            var categoryNameUpdateStatuses = new List<Task<bool>>();

            if (!string.IsNullOrEmpty(changes.Name) && !string.IsNullOrEmpty(changes.ItemId))
            {
                var updates = new Dictionary<string, object>
                {
                    {"CategoryName", changes.Name }
                };

                var builders = Builders<BsonDocument>.Filter;
                var dataFilters = builders.Eq("CategoryId", changes.ItemId);

                var praxisTaskUpdateStatus = changeLogService.UpdateChange(EntityName.PraxisTask, dataFilters, updates);
                categoryNameUpdateStatuses.Add(praxisTaskUpdateStatus);

                var openItemUpdateStatus =
                    changeLogService.UpdateChange(EntityName.PraxisOpenItem, dataFilters, updates);
                categoryNameUpdateStatuses.Add(openItemUpdateStatus);

                var riskUpdateStatus =
                    changeLogService.UpdateChange(EntityName.PraxisRisk, dataFilters, updates);
                categoryNameUpdateStatuses.Add(riskUpdateStatus);

                var equiopmentUpdateStatus =
                    changeLogService.UpdateChange(EntityName.PraxisEquipment, dataFilters, updates);
                categoryNameUpdateStatuses.Add(equiopmentUpdateStatus);
            }

            if (changes.Subcategories.Any())
            {
                foreach (SubCategoryModel model in changes.Subcategories)
                {
                    if (!string.IsNullOrEmpty(model.ItemId) && !string.IsNullOrEmpty(model.Name))
                    {
                        var updates = new Dictionary<string, object>
                        {
                            {"SubCategoryName", model.Name }
                        };

                        var builders = Builders<BsonDocument>.Filter;
                        var dataFilters = builders.Eq("SubCategoryId", model.ItemId);

                        var praxisTaskUpdateStatus = changeLogService.UpdateChange(EntityName.PraxisTask, dataFilters, updates);
                        categoryNameUpdateStatuses.Add(praxisTaskUpdateStatus);

                        var openItemUpdateStatus =
                            changeLogService.UpdateChange(EntityName.PraxisOpenItem, dataFilters, updates);
                        categoryNameUpdateStatuses.Add(openItemUpdateStatus);

                        var riskUpdateStatus =
                            changeLogService.UpdateChange(EntityName.PraxisRisk, dataFilters, updates);
                        categoryNameUpdateStatuses.Add(riskUpdateStatus);

                        var equiopmentUpdateStatus =
                            changeLogService.UpdateChange(EntityName.PraxisEquipment, dataFilters, updates);
                        categoryNameUpdateStatuses.Add(equiopmentUpdateStatus);
                    }
                }
            }

            await Task.WhenAll(categoryNameUpdateStatuses);

            return true;
        }
    }
}
