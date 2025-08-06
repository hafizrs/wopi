using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.PraxisOpenItem;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers;

public class GetPraxisOpenItemsQueryHandler : IQueryHandler<GetPraxisOpenItemsQuery, QueryHandlerResponse>
{
    private readonly ILogger<GetPraxisOpenItemsQueryHandler> _logger;
    private readonly IPraxisOpenItemService _praxisOpenItemService;

    public GetPraxisOpenItemsQueryHandler(
        ILogger<GetPraxisOpenItemsQueryHandler> logger,
        IPraxisOpenItemService praxisOpenItemService
    )
    {
        _logger = logger;
        _praxisOpenItemService = praxisOpenItemService;
    }

    public QueryHandlerResponse Handle(GetPraxisOpenItemsQuery query)
    {
        throw new NotImplementedException();
    }

    public async Task<QueryHandlerResponse> HandleAsync(GetPraxisOpenItemsQuery query)
    {
        _logger.LogInformation("Enter in {HandlerName} with query: {QueryName}.",
            nameof(GetPraxisOpenItemsQueryHandler), query);
        try
        {
            var response = await _praxisOpenItemService.GetPraxisOpenItems(query.Filter, query.Sort, query.PageNumber,
                    query.PageSize);
            var count = query.PageNumber == 0 ? await _praxisOpenItemService.GetOpenItemDocumentCount(query.Filter) : 0;
            return new QueryHandlerResponse
            {
                Results = response,
                TotalCount = count,
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                nameof(GetPraxisOpenItemsQueryHandler), e.Message, e.StackTrace);
            throw;
        }
    }
}