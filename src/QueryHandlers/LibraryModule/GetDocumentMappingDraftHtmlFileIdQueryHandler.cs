using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetDocumentMappingDraftHtmlFileIdQueryHandler : IQueryHandler<GetDocumentMappingDraftHtmlFileIdQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetDocumentMappingDraftHtmlFileIdQueryHandler> _logger;
        private readonly IDocumentEditMappingService _documentEditMappingService;

        public GetDocumentMappingDraftHtmlFileIdQueryHandler(
            ILogger<GetDocumentMappingDraftHtmlFileIdQueryHandler> logger,
            IDocumentEditMappingService documentEditMappingService
            )
        {
            _logger = logger;
            this._documentEditMappingService = documentEditMappingService;
        }

        public QueryHandlerResponse Handle(GetDocumentMappingDraftHtmlFileIdQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetDocumentMappingDraftHtmlFileIdQuery query)
        {
            _logger.LogInformation("Enter in {HandlerName} with query: {QueryName}.",
                nameof(GetDocumentMappingDraftHtmlFileIdQueryHandler), JsonConvert.SerializeObject(query));
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var response = new QueryHandlerResponse();

            try
            {
                var documentMappingData = await _documentEditMappingService.GetDocumentEditMappingRecordByDraftArtifact(query.ObjectArtifactId);
                if (documentMappingData != null)
                {
                    response.Data = documentMappingData.CurrentHtmlFileId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetDocumentMappingDraftHtmlFileIdQueryHandler), ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetDocumentMappingDraftHtmlFileIdQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }

}
