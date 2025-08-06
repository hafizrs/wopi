using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.ErrorResponse;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.AbsenceModule
{
    public class GetAbsencePlanApprovalPermissionQueryHandler : IQueryHandler<GetAbsencePlanApprovalPermissionQuery, QueryHandlerResponse>
    {
        private readonly IAbsenceOverviewService _absenceOverviewService;
        private readonly ILogger<GetAbsencePlanApprovalPermissionQueryHandler> _logger;

        public GetAbsencePlanApprovalPermissionQueryHandler(
            IAbsenceOverviewService absenceOverviewService,
            ILogger<GetAbsencePlanApprovalPermissionQueryHandler> logger)
        {
            _absenceOverviewService = absenceOverviewService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetAbsencePlanApprovalPermissionQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetAbsencePlanApprovalPermissionQuery query)
        {
            _logger.LogInformation("Entered into {HandlerName} with query: {@Query}", 
                nameof(GetAbsencePlanApprovalPermissionQueryHandler), JsonConvert.SerializeObject(query, Formatting.Indented));
            var response = new QueryHandlerResponse();
            try
            {
                var result = await _absenceOverviewService.GetAbsencePlanApprovalPermissionAsync(query);
                response.Data = result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Message: {Message}", 
                    nameof(GetAbsencePlanApprovalPermissionQueryHandler), ex.Message);
               response.ErrorMessage = ErrorResponseBuilder.FromException(ex).ToString();
            }
            return response;
        }
    }
} 