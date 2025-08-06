using System;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.LibraryModule;

public class GetItemsUsageInEntitiesQueryHandler : IQueryHandler<GetItemsUsageInEntitiesQuery, QueryHandlerResponse>
{
    private readonly ILogger<GetItemsUsageInEntitiesQueryHandler> _logger;
    private readonly IItemsUsageInEntitiesQueryService _itemsUsageInEntitiesQueryService;

    public GetItemsUsageInEntitiesQueryHandler(
        ILogger<GetItemsUsageInEntitiesQueryHandler> logger,
        IItemsUsageInEntitiesQueryService itemsUsageInEntitiesQueryService)
    {
        _logger = logger;
        _itemsUsageInEntitiesQueryService = itemsUsageInEntitiesQueryService;
    }
    public QueryHandlerResponse Handle(GetItemsUsageInEntitiesQuery query)
    {
        throw new System.NotImplementedException();
    }

    public async Task<QueryHandlerResponse> HandleAsync(GetItemsUsageInEntitiesQuery query)
    {
        _logger.LogInformation("Entered in Query Handler {HandlerName} with query: {Query}",
            nameof(GetItemsUsageInEntitiesQueryHandler), JsonConvert.SerializeObject(query));
        var response = new QueryHandlerResponse();
        try
        {
            if (query.EntityName != nameof(PraxisEquipmentMaintenance))
            {
                throw new Exception("Invalid Entity Name. Query is designed to get results only for PraxisEquipmentMaintenance");
            }
            response.Results = await _itemsUsageInEntitiesQueryService.GetItemsUsageInEntities(query);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in Query Handler {HandlerName} Exception Message: {Message} Exception details: {StackTrace}.",
                nameof(GetItemsUsageInEntitiesQueryHandler), e.Message, e.StackTrace);
            response.ErrorMessage = e.Message;
            response.StatusCode = 1;
        }
        return response;
    }
}