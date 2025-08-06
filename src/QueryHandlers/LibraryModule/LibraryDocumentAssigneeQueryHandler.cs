using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.LibraryModule
{
    public class LibraryDocumentAssigneeQueryHandler : IQueryHandler<LibraryDocumentAssigneeQuery, QueryHandlerResponse>
    {
        private readonly ILogger<LibraryDocumentAssigneeQueryHandler> _logger;
        private readonly ILibraryDocumentAssigneeService _libraryDocumentAssigneeService;

        public LibraryDocumentAssigneeQueryHandler(
            ILogger<LibraryDocumentAssigneeQueryHandler> logger,
            ILibraryDocumentAssigneeService libraryDocumentAssigneeService)
        {
            _logger = logger;
            _libraryDocumentAssigneeService = libraryDocumentAssigneeService;
        }

        public QueryHandlerResponse Handle(LibraryDocumentAssigneeQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(LibraryDocumentAssigneeQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with Query: {query}",
                nameof(LibraryDocumentAssigneeQueryHandler), query);

            var response = new QueryHandlerResponse();
            try
            {
                if (!string.IsNullOrEmpty(query.ObjectArtifactId))
                {
                    response.StatusCode = 0;
                    response.Results = await _libraryDocumentAssigneeService.GetPurposeWiseLibraryAssignees(query);
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;

                _logger.LogInformation("Exception in the query handler {HandlerName}. Exception Message: {ExceptionMessage}. Exception details: {ExceptionDetails}",
                    nameof(LibraryDocumentAssigneeQueryHandler), ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled by {HandlerName}", nameof(LibraryDocumentAssigneeQueryHandler));
            return response;
        }
    }
}
