using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetCurrentStatusQueryHandler : IQueryHandler<GetCurrentStatusQuery, CurrentStatusResponse>
    {
        private readonly ILogger<GetCurrentStatusQueryHandler> _logger;
        private readonly ICurrentStatusStrategy _currentStatusStrategyService;

        public GetCurrentStatusQueryHandler(
            ILogger<GetCurrentStatusQueryHandler> logger,
            ICurrentStatusStrategy currentStatusStrategyService)
        {
            _logger = logger;
            _currentStatusStrategyService = currentStatusStrategyService;
        }
        [Invocable]
        public CurrentStatusResponse Handle(GetCurrentStatusQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
                nameof(GetCurrentStatusQueryHandler), JsonConvert.SerializeObject(query));
            
            var currentStatusService = _currentStatusStrategyService.GetType(query.EntityName);
            var result = currentStatusService.DataCount(query);
            
            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(GetCurrentStatusQueryHandler), JsonConvert.SerializeObject(result));
            return result;
        }

        public Task<CurrentStatusResponse> HandleAsync(GetCurrentStatusQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
