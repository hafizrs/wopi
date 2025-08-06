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
    public class GetAbsencePlanByIdQueryHandler : IQueryHandler<GetAbsencePlanByIdQuery, QueryHandlerResponse>
    {
        private readonly IAbsenceOverviewService _absenceOverviewService;
        private readonly ILogger<GetAbsencePlanByIdQueryHandler> _logger;

        public GetAbsencePlanByIdQueryHandler(
            IAbsenceOverviewService absenceOverviewService,
            ILogger<GetAbsencePlanByIdQueryHandler> logger)
        {
            _absenceOverviewService = absenceOverviewService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetAbsencePlanByIdQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetAbsencePlanByIdQuery query)
        {
            _logger.LogInformation("Entered into {HandlerName} with query: {@Query}", 
                nameof(GetAbsencePlanByIdQueryHandler), JsonConvert.SerializeObject(query, Formatting.Indented));
            var response = new QueryHandlerResponse();
            try
            {
                var result = await _absenceOverviewService.GetAbsencePlanByIdAsync(query);
                response.Data = result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}", nameof(GetAbsencePlanByIdQueryHandler));
                response.ErrorMessage = ErrorResponseBuilder.FromException(ex).ToString();
            }
            return response;
        }
    }
}