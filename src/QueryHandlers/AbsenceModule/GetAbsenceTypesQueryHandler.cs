using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.ErrorResponse;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.AbsenceModule
{
    public class GetAbsenceTypesQueryHandler : IQueryHandler<GetAbsenceTypesQuery, QueryHandlerResponse>
    {
        private readonly IAbsenceOverviewService _absenceOverviewService;
        private readonly ILogger<GetAbsenceTypesQueryHandler> _logger;

        public GetAbsenceTypesQueryHandler(
            IAbsenceOverviewService absenceOverviewService,
            ILogger<GetAbsenceTypesQueryHandler> logger)
        {
            _absenceOverviewService = absenceOverviewService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetAbsenceTypesQuery query)
        {
            throw new NotImplementedException();
        }

        public Task<QueryHandlerResponse> HandleAsync(GetAbsenceTypesQuery query)
        {
            _logger.LogInformation("Entered into {HandlerName} with query: {@Query}", 
                nameof(GetAbsenceTypesQueryHandler), JsonConvert.SerializeObject(query, Formatting.Indented));
            var response = new QueryHandlerResponse();
            try
            {
                response.Results = _absenceOverviewService.GetAbsenceTypes(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving absence types for query: {@Query}", query);
                response.ErrorMessage = ErrorResponseBuilder.FromException(ex).ToString();
            }
            return Task.FromResult(response);
        }
    }
}