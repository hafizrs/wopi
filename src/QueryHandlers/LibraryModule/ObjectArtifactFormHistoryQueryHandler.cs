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
    public class ObjectArtifactFormHistoryQueryHandler : IQueryHandler<ObjectArtifactFormHistoryQuery, QueryHandlerResponse>
    {
        private readonly ILogger<ObjectArtifactFormHistoryQueryHandler> _logger;
        private readonly IObjectArtifactFormHistoryService _objectArtifactFormHistoryService;

        public ObjectArtifactFormHistoryQueryHandler(
            ILogger<ObjectArtifactFormHistoryQueryHandler> logger,
            IObjectArtifactFormHistoryService objectArtifactFormHistoryService)
        {
            _logger = logger;
            _objectArtifactFormHistoryService = objectArtifactFormHistoryService;
        }

        public QueryHandlerResponse Handle(ObjectArtifactFormHistoryQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(ObjectArtifactFormHistoryQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with qu{QueryName} query",
                nameof(ObjectArtifactFormHistoryQueryHandler), JsonConvert.SerializeObject(query));
            
            var response = new QueryHandlerResponse();
            try
            {
                if (!string.IsNullOrEmpty(query.ObjectArtifactId))
                {
                    response.StatusCode = 0;
                    response.Results = await _objectArtifactFormHistoryService.GetObjectArtifactFormHistory(query);
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;

                _logger.LogError(
                    "Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(ObjectArtifactFormHistoryQueryHandler), ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(ObjectArtifactFormHistoryQueryHandler), JsonConvert.SerializeObject(response));
            return response;
        }
    }
}
