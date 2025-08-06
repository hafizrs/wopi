using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class SignatureUrlGetQueryHandler : IQueryHandler<SignatureUrlGetQuery, QueryHandlerResponse>
    {
        private readonly ILibraryFormService _libraryFormService;
        private readonly ILogger<SignatureUrlGetQueryHandler> _logger;

        public SignatureUrlGetQueryHandler(
            ILibraryFormService libraryFormService,
            ILogger<SignatureUrlGetQueryHandler> logger
        )
        {
            _libraryFormService = libraryFormService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(SignatureUrlGetQuery query)
        {
            return HandleAsync(query).Result;
        }

        public async Task<QueryHandlerResponse> HandleAsync(SignatureUrlGetQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(SignatureUrlGetQueryHandler), query);
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
                response.Data = await _libraryFormService.GetFormSignatureMapping(query.ObjectArtifactId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(SignatureUrlGetQueryHandler), ex.Message, ex.StackTrace);

                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(SignatureUrlGetQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
