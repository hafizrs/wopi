using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class LibraryFormCloneGetQueryHandler : IQueryHandler<LibraryFormCloneGetQuery,
        QueryHandlerResponse>
    {
        private readonly ILogger<LibraryFormCloneGetQueryHandler> _logger;
        private readonly ILibraryFormService _libraryFormService;

        public LibraryFormCloneGetQueryHandler(
            ILogger<LibraryFormCloneGetQueryHandler> logger,
            ILibraryFormService libraryFormService
        )
        {
            _logger = logger;
            _libraryFormService = libraryFormService;
        }

        public QueryHandlerResponse Handle(LibraryFormCloneGetQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(LibraryFormCloneGetQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(LibraryFormCloneGetQueryHandler), JsonConvert.SerializeObject(query));
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
                var documentMappingData =
                    await _libraryFormService.GetFormCloneMappingRecord(query.ObjectArtifactId);
                if (documentMappingData != null)
                {
                    response.Data = documentMappingData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(LibraryFormCloneGetQueryHandler), ex.Message, ex.StackTrace);

                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(LibraryFormCloneGetQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
