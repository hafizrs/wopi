using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.ConfiguratorModule;

public class GetPastReportSummariesQueryHandler : IQueryHandler<GetPastReportSummariesQuery, QueryHandlerResponse>
{
    private readonly ILogger<GetPastReportSummariesQueryHandler> _logger;
    private readonly IPraxisReportTemplateService _praxisReportTemplateService;
    public GetPastReportSummariesQueryHandler(
        ILogger<GetPastReportSummariesQueryHandler> logger,
        IPraxisReportTemplateService praxisReportTemplateService)
    {
        _logger = logger;
        _praxisReportTemplateService = praxisReportTemplateService;
    }
    public QueryHandlerResponse Handle(GetPastReportSummariesQuery query)
    {
        throw new System.NotImplementedException();
    }

    public async Task<QueryHandlerResponse> HandleAsync(GetPastReportSummariesQuery query)
    {
        _logger.LogInformation("Entered into {HandlerName} with query: {Query}", nameof(GetPastReportSummariesQueryHandler), query);
        var response = new QueryHandlerResponse();
        try
        {
            response = await _praxisReportTemplateService.GetPastReportSummary(query);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in {HandlerName}. Error Message -> {Message}. Error Details -> {StackTrace}", nameof(GetPastReportSummariesQueryHandler), e.Message, e.StackTrace);
            response.StatusCode = 1;
            response.ErrorMessage = e.Message;
        }
        return response;
    }
}