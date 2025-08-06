using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.CirsReports;

public class GetCirsAdminsQueryHandler : AbstractQueryHandler<GetCirsAdminsQuery>
{
    private readonly ILogger<GetCirsAdminsQueryHandler> _logger;
    private readonly IGetCirsAdminsService _getCirsAdminsService;

    public GetCirsAdminsQueryHandler(
        ILogger<GetCirsAdminsQueryHandler> logger,
        IGetCirsAdminsService getCirsAdminsService)
    {
        _logger = logger;
        _getCirsAdminsService = getCirsAdminsService;
    }

    public override async Task<QueryHandlerResponse> HandleAsync(GetCirsAdminsQuery query)
    {
        QueryHandlerResponse response = new QueryHandlerResponse();

        _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
            nameof(GetCirsAdminsQueryHandler), JsonConvert.SerializeObject(query));

        try
        {
            List<CirsAdminsResponse> admins = await _getCirsAdminsService.GetCirsAdmins(query);
            response.Data = admins;
            response.StatusCode = 0;
            response.TotalCount = admins.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                nameof(GetCirsAdminsQueryHandler), ex.Message, ex.StackTrace);
            response.StatusCode = 1;
            response.ErrorMessage = ex.Message;
        }

        _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
            nameof(GetCirsAdminsQueryHandler), JsonConvert.SerializeObject(response));

        return response;
    }
}
