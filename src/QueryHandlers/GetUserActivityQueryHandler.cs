using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetUserActivityQueryHandler : IQueryHandler<GetUserActivityQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetCurrentStatusQueryHandler> _logger;
        private readonly IUserActivityService _userActivityService;
        public GetUserActivityQueryHandler(
           ILogger<GetCurrentStatusQueryHandler> logger,
           IUserActivityService userActivityService
        ) 
        { 
           _logger = logger;
           _userActivityService = userActivityService;  
        }

        public QueryHandlerResponse Handle(GetUserActivityQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetUserActivityQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var result = await _userActivityService.GetUserActivity(query.userId, query.action);
            return new QueryHandlerResponse
            {
                Results = result,
                TotalCount = 1
            };
        }
    }
}
