using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetLibraryGroupsQueryHandler : IQueryHandler<GetLibraryGroupsQuery, QueryHandlerResponse>
    {
        private readonly IGetLibraryGroupsService _getLibraryGroupsService;
        private readonly ILogger<GetLibraryGroupsQueryHandler> _logger;

        public GetLibraryGroupsQueryHandler
        (
            IGetLibraryGroupsService getLibraryGroupsService,
            ILogger<GetLibraryGroupsQueryHandler> logger
        )
        {
            _getLibraryGroupsService = getLibraryGroupsService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetLibraryGroupsQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetLibraryGroupsQuery query)
        {
            var response = new QueryHandlerResponse();

            try
            {
                _logger.LogInformation("Enter {HandlerName} with query: {Query}",
                    nameof(GetLibraryGroupsQueryHandler), JsonConvert.SerializeObject(query));

                var libraryResponse = await _getLibraryGroupsService.GetLibraryGroupsAsync(query);

                response.Data = libraryResponse;

                _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                    nameof(GetLibraryGroupsQueryHandler), JsonConvert.SerializeObject(response));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetLibraryGroupsQueryHandler), ex.Message, ex.StackTrace);

                response.ErrorMessage = ex.Message;
            }

            return response;
        }
    }
}
