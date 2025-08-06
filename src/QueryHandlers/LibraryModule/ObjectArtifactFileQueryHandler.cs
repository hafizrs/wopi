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
    public class ObjectArtifactFileQueryHandler : IQueryHandler<ObjectArtifactFileQuery, QueryHandlerResponse>
    {
        private readonly ILogger<ObjectArtifactFileQueryHandler> _logger;
        private readonly IObjectArtifactFileQueryService _objectArtifactFileQueryService;
         
        public ObjectArtifactFileQueryHandler(
            ILogger<ObjectArtifactFileQueryHandler> logger,
            IObjectArtifactFileQueryService objectArtifactFileQueryService)
        {
            _logger = logger;
            _objectArtifactFileQueryService = objectArtifactFileQueryService;
        }

        public QueryHandlerResponse Handle(ObjectArtifactFileQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(ObjectArtifactFileQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with Query: {query}", nameof(ObjectArtifactFileQueryHandler),
                JsonConvert.SerializeObject(query));
            
            var response = new QueryHandlerResponse();
            try
            {
                response.StatusCode = 0;
                response.Results = await _objectArtifactFileQueryService.InitiateGetFileArtifacts(query);
            }
            catch (Exception ex)
            {
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;

                _logger.LogInformation("Exception in the query handler {HandlerName}. Exception Message: {ExceptionMessage}. Exception details: {ExceptionDetails}",
                    nameof(ObjectArtifactFileQueryHandler), ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled by {HandlerName}", nameof(ObjectArtifactFileQueryHandler));
            return response;
        }
    }
}
