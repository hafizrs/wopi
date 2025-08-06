using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;
using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.CirsReports;

public class GetUserCirsPermissionInfoQueryHandler : AbstractQueryHandler<GetUserCirsPermissionInfoQuery>
{
    private readonly ILogger<GetUserCirsPermissionInfoQueryHandler> _logger;
    private readonly ICirsPermissionService _cirsPermissionService;

    public GetUserCirsPermissionInfoQueryHandler(
        ICirsPermissionService cirsPermissionService,
        ILogger<GetUserCirsPermissionInfoQueryHandler> logger)
    {
        _cirsPermissionService = cirsPermissionService;
        _logger = logger;
    }

    public override async Task<QueryHandlerResponse> HandleAsync(GetUserCirsPermissionInfoQuery query)
    {
        try
        {
            var response = new QueryHandlerResponse();
            if (!string.IsNullOrEmpty(query.PraxisClientId) && !string.IsNullOrEmpty(query.DashboardName))
            {
                var permission = await _cirsPermissionService.GetCirsDashboardPermissionAsync(query.PraxisClientId, query.DashboardNameEnum, true);

                var responseData = new UserCirsPermissionInfoResponse
                {
                    AssignmentLevel = ((int)permission.AssignmentLevel).ToString(),
                    LoggedInUserPermission = _cirsPermissionService.GetPermissionsByDashBoardName(query.DashboardNameEnum, permission)
                };

                response.Data = responseData;
                response.StatusCode = 0;
                response.TotalCount = 1;
            }
            else
            {
                response.StatusCode = 1;
                response.ErrorMessage = "Required Field Missing!";
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in {HandlerName}. Error Message -> {Message}. Error Details -> {StackTrace}",
                   nameof(GetUserCirsPermissionInfoQuery), ex.Message, ex.StackTrace);

            return new QueryHandlerResponse
            {
                Data = null,
                StatusCode = 1,
                ErrorMessage = ex.Message
            };
        }
       
    }
}
