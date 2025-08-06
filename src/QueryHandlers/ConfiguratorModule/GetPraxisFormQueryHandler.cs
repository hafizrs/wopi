using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.PraxisOpenItem;
using Selise.Ecap.SC.PraxisMonitor.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers;

public class GetPraxisFormQueryHandler : IQueryHandler<GetPraxisFormQuery, QueryHandlerResponse>
{
    private readonly ILogger<GetPraxisFormQueryHandler> _logger;
    private readonly IPraxisFormService _praxisFormService;

    public GetPraxisFormQueryHandler(
        ILogger<GetPraxisFormQueryHandler> logger,
        IPraxisFormService praxisFormService
    )
    {
        _logger = logger;
        _praxisFormService = praxisFormService;
    }

    public QueryHandlerResponse Handle(GetPraxisFormQuery query)
    {
        throw new NotImplementedException();
    }

    public async Task<QueryHandlerResponse> HandleAsync(GetPraxisFormQuery query)
    {
        _logger.LogInformation("Enter in {HandlerName} with query: {QueryName}.",
            nameof(GetPraxisFormQueryHandler), query);
        try
        {
            var response = await _praxisFormService.GetPraxisFormDetailWithPermission(query.ItemId);
          
            return new QueryHandlerResponse
            {
                Results = response.Results,
                TotalCount = response.TotalRecordCount,
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                nameof(GetPraxisFormQueryHandler), e.Message, e.StackTrace);
            throw;
        }
    }
}