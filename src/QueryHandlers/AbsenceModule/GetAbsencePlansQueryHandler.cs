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
    public class GetAbsencePlansQueryHandler : IQueryHandler<GetAbsencePlansQuery, QueryHandlerResponse>
    {
        private readonly IAbsenceOverviewService _absenceOverviewService;
        private readonly ILogger<GetAbsencePlansQueryHandler> _logger;

        public GetAbsencePlansQueryHandler(
            IAbsenceOverviewService absenceOverviewService,
            ILogger<GetAbsencePlansQueryHandler> logger)
        {
            _absenceOverviewService = absenceOverviewService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetAbsencePlansQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetAbsencePlansQuery query)
        {
            _logger.LogInformation("Entered into {HandlerName} with query: {@Query}", 
                nameof(GetAbsencePlansQueryHandler), JsonConvert.SerializeObject(query, Formatting.Indented));
            var response = new QueryHandlerResponse();
            try
            {
                var absencePlans = await _absenceOverviewService.GetAbsencePlansAsync(query);
                response.Data = absencePlans;
                response.TotalCount = absencePlans.Count;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in {HandlerName}. Message: {Message}", nameof(GetAbsencePlansQueryHandler), ex.Message);
                response.ErrorMessage = ErrorResponseBuilder.FromException(ex).ToString();
            }
            return response;
        }
    }
}