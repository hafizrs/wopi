using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ClientModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.ClientModule
{
    public class GetMalfunctionGroupQueryHandler : IQueryHandler<GetMalfunctionGroupQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetMalfunctionGroupQueryHandler> _logger;
        private readonly IPraxisClientMalfunctionGroupService _praxisClientMalfunctionGroupService;

        public GetMalfunctionGroupQueryHandler(
            ILogger<GetMalfunctionGroupQueryHandler> logger,
            IPraxisClientMalfunctionGroupService praxisClientMalfunctionGroupService)
        {
            _logger = logger;
            _praxisClientMalfunctionGroupService = praxisClientMalfunctionGroupService;
        }

        public QueryHandlerResponse Handle(GetMalfunctionGroupQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetMalfunctionGroupQuery query)
        {
            _logger.LogInformation("Entered into Handler: {HandlerName} with query: {Query}", nameof(GetMalfunctionGroupQueryHandler), JsonConvert.SerializeObject(query));
            var response = new QueryHandlerResponse();
            try
            {
                var malfunctionGroups = await _praxisClientMalfunctionGroupService.GetMalfunctionGroupsAsync(query);
                response.Results = malfunctionGroups;
                response.TotalCount = malfunctionGroups?.Count() ?? 0;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in Handler: {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}",
                    nameof(GetMalfunctionGroupQueryHandler), e.Message, e.StackTrace);
                response.ErrorMessage = $"Message: {e.Message}. Details: {e.StackTrace}";
            }
            return response;
        }
    }
}
