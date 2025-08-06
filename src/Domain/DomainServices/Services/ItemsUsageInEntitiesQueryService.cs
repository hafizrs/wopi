using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices;

public class ItemsUsageInEntitiesQueryService : IItemsUsageInEntitiesQueryService
{
    private readonly ILogger<ItemsUsageInEntitiesQueryService> _logger;
    private readonly IRepository _repository;

    public ItemsUsageInEntitiesQueryService(ILogger<ItemsUsageInEntitiesQueryService> logger, IRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public Task<List<GetItemsUsageInEntitiesQueryDto>> GetItemsUsageInEntities(GetItemsUsageInEntitiesQuery query)
    {
        var response = new List<GetItemsUsageInEntitiesQueryDto>();

        switch (query.EntityName)
        {
            case nameof(PraxisEquipmentMaintenance):
                foreach (var entityNameModel in query.QueryItems)
                {
                    if (entityNameModel.QueryItemIds?.Count == 0 ||
                        string.IsNullOrEmpty(entityNameModel.EntityItemId) ||
                        string.IsNullOrEmpty(entityNameModel.PropertyName))
                    {
                        const string message = "Invalid Query Items. Query Items should have at least one Query Item Id, Entity Item Id and Property Name";
                        response.Add(new GetItemsUsageInEntitiesQueryDto
                        {
                            ErrorMessage = message
                        });
                        continue;
                    }

                    entityNameModel.QueryItemIds ??= new List<string>();
                    switch (entityNameModel.PropertyName)
                    {
                        case nameof(PraxisForm):
                            response.AddRange(entityNameModel.QueryItemIds
                                .Select(f => GetFormsUsageInPraxisEquipmentMaintenance(f, entityNameModel.EntityItemId)));
                            break;
                        case "LibraryForm":
                            response.AddRange(entityNameModel.QueryItemIds
                                .Select(f => GetLibraryFormsUsageInPraxisEquipmentMaintenance(f, entityNameModel.EntityItemId)));
                            break;
                    }
                }
                break;
            default:
                throw new Exception("Invalid Entity Name. Query is designed to get results only for PraxisEquipmentMaintenance");
        }

        

        return Task.FromResult(response);
    }

    private GetItemsUsageInEntitiesQueryDto GetFormsUsageInPraxisEquipmentMaintenance(string itemId, string equipmentId)
    {
        var activeMaintenances = _repository.GetItems<PraxisEquipmentMaintenance>(pem =>
                !pem.IsMarkedToDelete &&
                !(pem.CompletionStatus != null &&
                  pem.CompletionStatus.Value == "Done") &&
                pem.PraxisFormInfo != null &&
                pem.PraxisFormInfo.FormId == itemId &&
                pem.PraxisEquipmentId == equipmentId)?
            .Select(p => new MatchedItemModel
            {
                CreateDate = p.MaintenanceDate,
                EndDate = p.MaintenanceEndDate,
                Id = p.ItemId,
                ScheduleType = p.ScheduleType,
            }).ToList() ?? new List<MatchedItemModel>();
        return new GetItemsUsageInEntitiesQueryDto
        {
            EntityName = nameof(PraxisEquipmentMaintenance),
            QueryItemId = itemId,
            MatchedItems = activeMaintenances,
            PropertyName = nameof(PraxisForm),
            TotalCount = activeMaintenances.Count
        };
    }
    private GetItemsUsageInEntitiesQueryDto GetLibraryFormsUsageInPraxisEquipmentMaintenance(string itemId, string equipmentId)
    {
        var activeMaintenances = _repository.GetItems<PraxisEquipmentMaintenance>(pem =>
                !pem.IsMarkedToDelete &&
                !(pem.CompletionStatus != null &&
                  pem.CompletionStatus.Value == "Done") &&
                pem.LibraryForms != null &&
                pem.LibraryForms.Any(f => f.LibraryFormId == itemId) &&
                pem.PraxisEquipmentId == equipmentId)?
            .Select(p => new MatchedItemModel
            {
                CreateDate = p.MaintenanceDate,
                EndDate = p.MaintenanceEndDate,
                Id = p.ItemId,
                ScheduleType = p.ScheduleType,
            }).ToList() ?? new List<MatchedItemModel>();
        return new GetItemsUsageInEntitiesQueryDto
        {
            EntityName = nameof(PraxisEquipmentMaintenance),
            QueryItemId = itemId,
            MatchedItems = activeMaintenances,
            PropertyName = "LibraryForm",
            TotalCount = activeMaintenances.Count
        };

    }
}