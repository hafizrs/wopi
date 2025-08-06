using System;
using System.Collections.Generic;
using System.Linq;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.LibraryModule
{
    public class GetDmsArtifactUsageReferenceQueryHandler : IQueryHandler<GetDmsArtifactUsageReferenceQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetDmsArtifactUsageReferenceQueryHandler> _logger;
        private readonly IDmsArtifactUsageReferenceQueryService _dmsArtifactUsageReferenceQueryService;

        public GetDmsArtifactUsageReferenceQueryHandler(
            ILogger<GetDmsArtifactUsageReferenceQueryHandler> logger,
            IDmsArtifactUsageReferenceQueryService dmsArtifactUsageReferenceQueryService)
        {
            _logger = logger;
            _dmsArtifactUsageReferenceQueryService = dmsArtifactUsageReferenceQueryService;
        }
        public QueryHandlerResponse Handle(GetDmsArtifactUsageReferenceQuery query)
        {
            throw new System.NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetDmsArtifactUsageReferenceQuery query)
        {
            _logger.LogInformation("Entered into {HandlerName} with Query: {Query}", 
                nameof(GetDmsArtifactUsageReferenceQueryHandler), JsonConvert.SerializeObject(query));
            var response = new QueryHandlerResponse();
            try
            {
                var result = await _dmsArtifactUsageReferenceQueryService.GetDmsArtifactUsageReference(query.ObjectArtifactId, query.ClientId);
                response.Data = result;
                response.StatusCode = 0;
                response.TotalCount = result.Count;

                _logger.LogInformation("Handled by {HandlerName}", nameof(GetDmsArtifactUsageReferenceQueryHandler));
                
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in the query handler {HandlerName} Exception Message: {Message} Exception details: {StackTrace}.", 
                                 nameof(GetDmsArtifactUsageReferenceQueryHandler), e.Message, e.StackTrace);
                
                response.ErrorMessage = e.Message;
                response.StatusCode = 1;
                return response;
            }
        }
    }
}