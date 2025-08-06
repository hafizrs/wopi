using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.LibraryModule
{
    public class ObjectArtifactQueryHandler : IQueryHandler<ObjectArtifactQuery, QueryHandlerResponse>
    {
        private readonly ILogger<ObjectArtifactQueryHandler> _logger;
        private readonly IObjectArtifactQueryService _stashQueryService;
         
        public ObjectArtifactQueryHandler(
            ILogger<ObjectArtifactQueryHandler> logger,
            IObjectArtifactQueryService stashQueryService)
        {
            _logger = logger;
            _stashQueryService = stashQueryService;
        }

        public QueryHandlerResponse Handle(ObjectArtifactQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(ObjectArtifactQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
                nameof(ObjectArtifactQueryHandler), JsonConvert.SerializeObject(query));
            
            var response = new QueryHandlerResponse();
            try
            {
                response.StatusCode = 0;
                response.Results = await _stashQueryService.GetObjectArtifacts(query);
            }
            catch (Exception ex)
            {
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;

                _logger.LogError(
                    "Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(ObjectArtifactQueryHandler), ex.Message, ex.StackTrace);
            }
            
            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(ObjectArtifactQueryHandler), JsonConvert.SerializeObject(response));
            return response;
        }
    }
}
