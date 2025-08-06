using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers;

public class GetPraxisFormForDepartmentQueryHandler : IQueryHandler<GetPraxisFormForDepartmentQuery, QueryHandlerResponse>
{
    private readonly ILogger<GetPraxisFormForDepartmentQueryHandler> _logger;
    private readonly IPraxisFormService _praxisFormService;

    public GetPraxisFormForDepartmentQueryHandler(
        ILogger<GetPraxisFormForDepartmentQueryHandler> logger,
        IPraxisFormService praxisFormService
    )
    {
        _logger = logger;
        _praxisFormService = praxisFormService;
    }

    public QueryHandlerResponse Handle(GetPraxisFormForDepartmentQuery query)
    {
        throw new NotImplementedException();
    }

    public async Task<QueryHandlerResponse> HandleAsync(GetPraxisFormForDepartmentQuery query)
    {
        _logger.LogInformation("Enter in {HandlerName} with query: {QueryName}.",
            nameof(GetPraxisFormForDepartmentQueryHandler), query);
        try
        {
            var response = await _praxisFormService.GetPraxisFormForDepartment(query);

            return new QueryHandlerResponse
            {
                Results = response.Results,
                TotalCount = response.TotalRecordCount,
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                nameof(GetPraxisFormForDepartmentQueryHandler), e.Message, e.StackTrace);
            throw;
        }
    }
}