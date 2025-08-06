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
    public class LibraryRightsGetQueryHandler : IQueryHandler<LibraryRightsGetQuery,
        QueryHandlerResponse>
    {
        private readonly ILogger<LibraryRightsGetQueryHandler> _logger;
        private readonly IAssignLibraryAdminsService _service;

        public LibraryRightsGetQueryHandler(
            ILogger<LibraryRightsGetQueryHandler> logger,
            IAssignLibraryAdminsService service
        )
        {
            _logger = logger;
            _service = service;
        }

        public QueryHandlerResponse Handle(LibraryRightsGetQuery query)
        {
            return HandleAsync(query).Result;
        }

        public async Task<QueryHandlerResponse> HandleAsync(LibraryRightsGetQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(LibraryRightsGetQueryHandler), JsonConvert.SerializeObject(query));
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
                var data = !string.IsNullOrEmpty(query.DepartmentId) ? await _service.GetLibraryRightsForDepartment(query) : 
                                    await _service.GetLibraryRights(query);
                if (data != null)
                {
                    response.Data = data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(LibraryRightsGetQueryHandler), ex.Message, ex.StackTrace);

                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(LibraryRightsGetQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
